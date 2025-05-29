'use client';

import React, { useState, useMemo } from 'react';
import { FileIcon, FolderIcon, MoreVerticalIcon } from 'lucide-react';
import Editor from '@monaco-editor/react';
import { toast } from 'sonner';

import { FileSystemEntity } from '@/shared/api/contracts/filesystem/requests';
import { SettingsPlatformBaseEntity } from '@/shared/api/contracts';
import { StorageType } from '@/shared/enums';
import {
  useCreateArchive,
  useDeleteDirectory,
  useDeleteFile,
  useDeleteFiles,
  useDirectory,
  useDownloadFile,
  useExtractArchive,
  useUploadFile,
  useFileContent,
  useSaveFileContent,
  useCreateDirectory,
  useCreateFile,
} from '@/shared/hooks/useFileSystem';
import { useSettingsPlatform } from '@/shared/hooks/useSettings';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/shared/ui/breadcrumb';
import { Button } from '@/shared/ui/button';
import { Checkbox } from '@/shared/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/shared/ui/dropdown-menu';
import { Input } from '@/shared/ui/input';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/shared/ui/table';
import { Textarea } from '@/shared/ui/textarea';

export function FileExplorer() {
  const { data: platform } = useSettingsPlatform() as { data: SettingsPlatformBaseEntity };
  const isS3Storage = platform?.storageType === StorageType.STORAGE_TYPE_S3;

  const [currentPath, setCurrentPath] = useState('');
  const [selectedItems, setSelectedItems] = useState<string[]>([]);
  const [editingFile, setEditingFile] = useState<{ path: string; content: string } | null>(null);
  const [isCreatingFolder, setIsCreatingFolder] = useState(false);
  const [isCreatingFile, setIsCreatingFile] = useState(false);
  const [newItemName, setNewItemName] = useState('');
  const [inputError, setInputError] = useState<string>('');
  const [itemsToDelete, setItemsToDelete] = useState<FileSystemEntity[]>([]);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  
  const { data: directory, isLoading, refetch } = useDirectory(currentPath);
  const { mutateAsync: getFileContent } = useFileContent();
  const { mutateAsync: saveFileContent } = useSaveFileContent();
  const createDirectoryMutation = useCreateDirectory();
  const createFileMutation = useCreateFile();

  const deleteDirectoryMutation = useDeleteDirectory();
  const createArchiveMutation = useCreateArchive();
  const extractArchiveMutation = useExtractArchive();
  const downloadFileMutation = useDownloadFile();
  const uploadFileMutation = useUploadFile();
  const deleteFilesMutation = useDeleteFiles();
  const deleteFileMutation = useDeleteFile();

  const normalizePath = (path: string): string => {
    // Заменяем все обратные слеши на прямые для единообразия
    return path.replace(/\\/g, '/');
  };

  const pathParts = normalizePath(currentPath).split('/').filter(Boolean);
  const getPathToIndex = (index: number) => {
    return pathParts.slice(0, index + 1).join('/');
  };

  const handlePathChange = (newPath: string) => {
    setCurrentPath(normalizePath(newPath));
    setSelectedItems([]);
  };

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      try {
        await uploadFileMutation.mutate({ path: currentPath, file });
        await refetch();
        toast.success('Файл загружен', {
          description: `Файл "${file.name}" успешно загружен`
        });
        // Сбрасываем значение input'а
        event.target.value = '';
      } catch (error) {
        toast.error('Ошибка при загрузке', {
          description: 'Не удалось загрузить файл'
        });
      }
    }
  };

  const handleItemSelect = (path: string) => {
    // Проверяем, является ли файл защищенным
    const item = directory?.find(item => item.path === path);
    if (item && isProtectedFile(item.name)) {
      toast.error('Невозможно выбрать системный файл', {
        description: 'Файл data.db является системным и не может быть выбран'
      });
      return;
    }
    
    setSelectedItems((prev) =>
      prev.includes(path) ? prev.filter((p) => p !== path) : [...prev, path],
    );
  };

  const handleEditFile = async (path: string) => {
    if (!path) return;
    
    try {
      const response = await getFileContent(path);
      setEditingFile({ 
        path, 
        content: response.data.data ?? '' 
      });
    } catch (error) {
      console.error('Failed to load file content:', error);
    }
  };

  const handleSaveFile = async () => {
    if (!editingFile) return;

    try {
      await saveFileContent({
        path: editingFile.path,
        content: editingFile.content
      });
      setEditingFile(null);
      await refetch();
      toast.success('Файл сохранен', {
        description: `Изменения в файле "${editingFile.path}" успешно сохранены`
      });
    } catch (error) {
      toast.error('Ошибка при сохранении', {
        description: 'Не удалось сохранить изменения в файле'
      });
      console.error('Failed to save file:', error);
    }
  };

  const isEditableFile = (fileName: string): boolean => {
    const lowerFileName = fileName.toLowerCase();
    // Проверяем наличие расширения (точки в имени файла, не считая точки в начале)
    const hasExtension = lowerFileName.indexOf('.') > 0;
    
    if (!hasExtension) return false;

    // Список запрещенных расширений
    const notEditableExtensions = [
      // Системные файлы
      '.db', '.zip', '.jar',
      // Изображения
      '.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.svg', '.ico',
      // Аудио
      '.mp3', '.wav', '.ogg', '.m4a', '.flac', '.aac',
      // Видео
      '.mp4', '.avi', '.mkv', '.mov', '.wmv', '.flv', '.webm',
      // Документы
      '.pdf', '.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx',
      // Архивы
      '.rar', '.7z', '.tar', '.gz',
      // системные файлы
      '.dll', '.exe', '.so', '.jsa'
    ];
    
    return !notEditableExtensions.some(ext => lowerFileName.endsWith(ext));
  };

  const getNotEditableReason = (fileName: string): string => {
    const lowerFileName = fileName.toLowerCase();
    // Проверяем наличие расширения
    if (lowerFileName.indexOf('.') <= 0) return 'без расширения';
    
    // Группы форматов
    const formatGroups = {
      '.db': 'база данных',
      '.zip': 'архив',
      '.jar': 'Java архив',
      '.rar': 'архив',
      '.7z': 'архив',
      '.tar': 'архив',
      '.gz': 'архив',
      '.jpg': 'изображение',
      '.jpeg': 'изображение',
      '.png': 'изображение',
      '.gif': 'изображение',
      '.bmp': 'изображение',
      '.webp': 'изображение',
      '.svg': 'изображение',
      '.ico': 'изображение',
      '.mp3': 'аудио',
      '.wav': 'аудио',
      '.ogg': 'аудио',
      '.m4a': 'аудио',
      '.flac': 'аудио',
      '.aac': 'аудио',
      '.mp4': 'видео',
      '.avi': 'видео',
      '.mkv': 'видео',
      '.mov': 'видео',
      '.wmv': 'видео',
      '.flv': 'видео',
      '.webm': 'видео',
      '.pdf': 'документ',
      '.doc': 'документ',
      '.docx': 'документ',
      '.xls': 'документ',
      '.xlsx': 'документ',
      '.ppt': 'документ',
      '.pptx': 'документ',
      '.dll': 'библиотека',
      '.exe': 'библиотека',
      '.so': 'библиотека',
      '.jsa': 'библиотека',
    };

    for (const [extension, type] of Object.entries(formatGroups)) {
      if (lowerFileName.endsWith(extension)) {
        return `${type} (${extension})`;
      }
    }
    
    return '';
  };

  const handleItemClick = async (item: FileSystemEntity) => {
    if (item.isDirectory) {
      handlePathChange(normalizePath(item.path));
    } else if (isEditableFile(item.name)) {
      await handleEditFile(item.path);
    } else {
      const fileType = getNotEditableReason(item.name);
      toast.error('Файл не может быть отредактирован', {
        description: `Файлы формата ${fileType} не подлежат редактированию в веб-интерфейсе`
      });
    }
  };

  const isProtectedFile = (fileName: string): boolean => {
    return fileName.toLowerCase() === 'data.db';
  };

  const handleDeleteItems = async () => {
    // Проверяем, нет ли защищенных файлов среди выбранных
    const protectedFiles = itemsToDelete.filter(item => isProtectedFile(item.name));
    if (protectedFiles.length > 0) {
      toast.error('Невозможно удалить системные файлы', {
        description: 'Файл data.db является системным и не может быть удален'
      });
      setIsDeleteDialogOpen(false);
      setItemsToDelete([]);
      return;
    }

    try {
      const paths = itemsToDelete.map(item => item.path);
      if (paths.length === 1) {
        const item = itemsToDelete[0];
        if (item.isDirectory) {
          await deleteDirectoryMutation.mutateAsync(item.path);
        } else {
          await deleteFileMutation.mutateAsync(item.path);
        }
      } else {
        await deleteFilesMutation.mutateAsync(paths);
      }
      setIsDeleteDialogOpen(false);
      setItemsToDelete([]);
      setSelectedItems([]);
      await refetch();
    } catch (error) {
      toast.error('Ошибка при удалении', {
        description: 'Не удалось удалить выбранные элементы'
      });
    }
  };

  const handleItemAction = async (item: FileSystemEntity, action: 'open' | 'download' | 'delete' | 'archive' | 'extract' | 'edit') => {
    switch (action) {
      case 'open':
        if (item.isDirectory) {
          handlePathChange(normalizePath(item.path));
        }
        break;
      case 'download':
        await downloadFileMutation.mutateAsync(item.path);
        break;
      case 'delete':
        if (isProtectedFile(item.name)) {
          toast.error('Невозможно удалить системный файл', {
            description: 'Файл data.db является системным и не может быть удален'
          });
          return;
        }
        setItemsToDelete([item]);
        setIsDeleteDialogOpen(true);
        break;
      case 'archive':
        await createArchiveMutation.mutateAsync(item.path);
        await refetch();
        toast.success('Архив создан', {
          description: `Архив успешно создан для "${item.name}"`
        });
        break;
      case 'extract':
        if (item.path.endsWith('.zip')) {
          await extractArchiveMutation.mutateAsync(item.path);
          await refetch();
          toast.success('Архив распакован', {
            description: `Архив "${item.name}" успешно распакован`
          });
        }
        break;
      case 'edit':
        if (isEditableFile(item.name)) {
          await handleEditFile(item.path);
        } else {
          const fileType = getNotEditableReason(item.name);
          toast.error('Файл не может быть отредактирован', {
            description: `Файлы формата ${fileType} не подлежат редактированию в веб-интерфейсе`
          });
        }
        break;
    }
  };

  const handleCreateArchiveFromSelected = async () => {
    if (selectedItems.length === 1) {
      // Если выбран один элемент, используем обычный метод
      await createArchiveMutation.mutateAsync(selectedItems[0]);
    } else if (selectedItems.length > 1) {
      // Если выбрано несколько элементов, используем multiple
      await createArchiveMutation.mutateAsync({
        type: 'multiple',
        paths: selectedItems
      });
    }
    await refetch();
    toast.success('Архив создан', {
      description: 'Архив успешно создан для выбранных элементов'
    });
    setSelectedItems([]);
  };

  const renderIcon = (isDirectory: boolean) => {
    return isDirectory ? (
      <FolderIcon className="h-4 w-4" />
    ) : (
      <FileIcon className="h-4 w-4" />
    );
  };

  const formatFileSize = (bytes?: number): string => {
    if (!bytes) return '-';
    
    const units = ['B', 'KB', 'MB', 'GB'];
    let size = bytes;
    let unitIndex = 0;

    while (size >= 1024 && unitIndex < units.length - 1) {
      size /= 1024;
      unitIndex++;
    }

    return `${size.toFixed(2)} ${units[unitIndex]}`;
  };

  const formatDate = (dateString?: string): string => {
    if (!dateString) return '-';
    
    const date = new Date(dateString);
    return date.toLocaleString('ru-RU', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getFileLanguage = (fileName: string): string => {
    const extension = fileName.split('.').pop()?.toLowerCase() || '';
    const languageMap: Record<string, string> = {
      'js': 'javascript',
      'jsx': 'javascript',
      'ts': 'typescript',
      'tsx': 'typescript',
      'json': 'json',
      'html': 'html',
      'css': 'css',
      'scss': 'scss',
      'less': 'less',
      'xml': 'xml',
      'yaml': 'yaml',
      'yml': 'yaml',
      'md': 'markdown',
      'sql': 'sql',
      'py': 'python',
      'sh': 'shell',
      'bash': 'shell',
      'php': 'php',
      'java': 'java',
      'c': 'c',
      'cpp': 'cpp',
      'cs': 'csharp',
      'go': 'go',
      'rs': 'rust',
      'rb': 'ruby',
      'pl': 'perl',
      'lua': 'lua',
      'swift': 'swift',
      'kt': 'kotlin',
      'r': 'r',
      'dart': 'dart',
    };
    
    return languageMap[extension] || 'plaintext';
  };

  const handleEditorChange = (value: string | undefined) => {
    setEditingFile(prev => prev ? { ...prev, content: value || '' } : null);
  };

  const filteredDirectory = useMemo(() => {
    if (!directory || !searchQuery) return directory;
    
    const query = searchQuery.toLowerCase();
    return directory.filter(item => 
      item.name.toLowerCase().includes(query)
    );
  }, [directory, searchQuery]);

  const renderTableContent = () => {
    if (isLoading) {
      return (
        <TableRow>
          <TableCell colSpan={5} className="text-center">
            Загрузка...
          </TableCell>
        </TableRow>
      );
    }

    if (!filteredDirectory || filteredDirectory.length === 0) {
      return (
        <TableRow>
          <TableCell colSpan={5} className="text-center">
            {!directory ? 'Ничего не найдено' : 'Нет результатов поиска'}
          </TableCell>
        </TableRow>
      );
    }

    return (
      <>
        {currentPath && (
          <TableRow>
            <TableCell>
              <Checkbox disabled />
            </TableCell>
            <TableCell
              className="cursor-pointer hover:bg-accent/50"
              onClick={() => {
                const parentPath = currentPath.split('/').slice(0, -1).join('/');
                handlePathChange(parentPath);
              }}
              colSpan={4}
            >
              <div className="flex items-center gap-2">
                <FolderIcon className="h-4 w-4" />
                ...
              </div>
            </TableCell>
          </TableRow>
        )}
        {filteredDirectory.map((item) => (
          <TableRow key={item.path}>
            <TableCell>
              <Checkbox
                checked={selectedItems.includes(item.path)}
                onCheckedChange={() => handleItemSelect(item.path)}
                disabled={isProtectedFile(item.name)}
              />
            </TableCell>
            <TableCell
              className="cursor-pointer hover:bg-accent/50"
              onClick={() => handleItemClick(item)}
            >
              <div className="flex items-center gap-2">
                {renderIcon(item.isDirectory)}
                <span className={isProtectedFile(item.name) ? "text-muted-foreground" : ""}>
                  {item.name}
                </span>
              </div>
            </TableCell>
            <TableCell>{formatFileSize(item.size)}</TableCell>
            <TableCell>{formatDate(item.modifiedAt)}</TableCell>
            <TableCell>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button 
                    variant="ghost" 
                    className="h-8 w-8 p-0"
                    disabled={isProtectedFile(item.name)}
                  >
                    <MoreVerticalIcon className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {item.isDirectory ? (
                    <>
                      <DropdownMenuItem
                        onClick={() => handleItemClick(item)}
                      >
                        Открыть
                      </DropdownMenuItem>
                      <DropdownMenuItem
                        onClick={() => handleItemAction(item, 'archive')}
                      >
                        Создать архив
                      </DropdownMenuItem>
                    </>
                  ) : (
                    <>
                      <DropdownMenuItem
                        onClick={() => handleItemAction(item, 'download')}
                      >
                        Скачать
                      </DropdownMenuItem>
                      {isEditableFile(item.name) && (
                        <DropdownMenuItem
                          onClick={() => handleItemClick(item)}
                        >
                          Редактировать
                        </DropdownMenuItem>
                      )}
                      {item.path.endsWith('.zip') && (
                        <DropdownMenuItem
                          onClick={() => handleItemAction(item, 'extract')}
                        >
                          Извлечь
                        </DropdownMenuItem>
                      )}
                    </>
                  )}
                  <DropdownMenuItem
                    className="text-destructive"
                    onClick={() => handleItemAction(item, 'delete')}
                  >
                    Удалить
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </TableCell>
          </TableRow>
        ))}
      </>
    );
  };

  const isItemExists = (name: string): boolean => {
    if (!directory) return false;
    return directory.some(item => item.name.toLowerCase() === name.toLowerCase());
  };

  const handleCreateItem = async (type: 'file' | 'directory') => {
    if (!newItemName) {
      toast.error('Введите имя');
      return;
    }

    if (isItemExists(newItemName)) {
      toast.error('Элемент уже существует', {
        description: `${type === 'directory' ? 'Папка' : 'Файл'} с именем "${newItemName}" уже существует в этой директории`
      });
      return;
    }

    if (type === 'file') {
      const notEditableReason = getNotEditableReason(newItemName);
      if (notEditableReason) {
        toast.error('Невозможно создать файл', {
          description: `Файлы формата ${notEditableReason} не могут быть созданы в веб-интерфейсе`
        });
        return;
      }
    }

    const path = currentPath ? `${currentPath}/${newItemName}` : newItemName;

    try {
      if (type === 'directory') {
        await createDirectoryMutation.mutateAsync(path);
        toast.success('Папка создана', {
          description: `Папка "${newItemName}" успешно создана`
        });
      } else {
        await createFileMutation.mutateAsync(path);
        toast.success('Файл создан', {
          description: `Файл "${newItemName}" успешно создан`
        });
      }
      await refetch();
      setNewItemName('');
      setIsCreatingFolder(false);
      setIsCreatingFile(false);
    } catch (error) {
      toast.error(`Ошибка при создании ${type === 'directory' ? 'папки' : 'файла'}`, {
        description: 'Проверьте правильность имени и попробуйте снова'
      });
    }
  };

  const handleNewItemNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setNewItemName(value);
    
    if (!value) {
      setInputError('');
      return;
    }

    if (isItemExists(value)) {
      setInputError(`Ты че тварь. Элемент с таким именем уже существует`);
      return;
    }

    if (isCreatingFile && getNotEditableReason(value)) {
      setInputError(`Файлы формата ${getNotEditableReason(value)} не могут быть созданы`);
      return;
    }

    setInputError('');
  };

  // Обработчики для диалогов создания
  const handleCreateFolderClick = () => {
    setNewItemName('');
    setInputError('');
    setIsCreatingFolder(true);
  };

  const handleCreateFileClick = () => {
    setNewItemName('');
    setInputError('');
    setIsCreatingFile(true);
  };

  const handleCloseCreateDialog = () => {
    setNewItemName('');
    setInputError('');
    setIsCreatingFolder(false);
    setIsCreatingFile(false);
  };

  if (isS3Storage) {
    return (
      <div className="relative">
        <div className="absolute inset-0 bg-background/80 backdrop-blur-sm z-10 flex items-center justify-center">
          <div className="text-center p-4">
            <h3 className="text-lg font-semibold mb-2">Файловый менеджер недоступен</h3>
            <p className="text-sm text-muted-foreground">
              У вас включено использование S3 хранилища. Файловый менеджер доступен только при использовании локального хранилища.
            </p>
          </div>
        </div>
        <div className="opacity-30 pointer-events-none">
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <Breadcrumb>
                <BreadcrumbList>
                  <BreadcrumbItem>
                    <BreadcrumbLink onClick={() => handlePathChange('')}>Главная</BreadcrumbLink>
                  </BreadcrumbItem>
                  {pathParts.map((part, index) => (
                    <React.Fragment key={part}>
                      <BreadcrumbSeparator>/</BreadcrumbSeparator>
                      <BreadcrumbItem>
                        {index === pathParts.length - 1 ? (
                          <BreadcrumbPage>{part}</BreadcrumbPage>
                        ) : (
                          <BreadcrumbLink
                            onClick={() => handlePathChange(getPathToIndex(index))}
                          >
                            {part}
                          </BreadcrumbLink>
                        )}
                      </BreadcrumbItem>
                    </React.Fragment>
                  ))}
                </BreadcrumbList>
              </Breadcrumb>

              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  onClick={handleCreateFolderClick}
                >
                  Создать папку
                </Button>
                <Button
                  variant="outline"
                  onClick={handleCreateFileClick}
                >
                  Создать файл
                </Button>
                <Input
                  type="file"
                  className="hidden"
                  id="file-upload"
                  onChange={handleFileUpload}
                />
                <Button
                  variant="outline"
                  onClick={() => document.getElementById('file-upload')?.click()}
                >
                  Загрузить
                </Button>
                {selectedItems.length > 0 && (
                  <>
                    <Button
                      variant="secondary"
                      onClick={handleCreateArchiveFromSelected}
                    >
                      Создать архив
                    </Button>
                    <Button
                      variant="destructive"
                      onClick={() => {
                        const itemsToDelete = directory?.filter(item => selectedItems.includes(item.path)) || [];
                        const protectedFiles = itemsToDelete.filter(item => isProtectedFile(item.name));
                        
                        if (protectedFiles.length > 0) {
                          toast.error('Невозможно удалить системные файлы', {
                            description: 'Файл data.db является системным и не может быть удален'
                          });
                          return;
                        }
                        
                        setItemsToDelete(itemsToDelete);
                        setIsDeleteDialogOpen(true);
                      }}
                    >
                      Удалить
                    </Button>
                  </>
                )}
              </div>
            </div>

            <div className="border rounded-md mb-4">
              <Input
                placeholder="Поиск..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full border-0 focus-visible:ring-0 focus-visible:ring-offset-0 rounded-none"
              />
            </div>

            <div className="border rounded-md">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-12">
                      <Checkbox
                        checked={
                          filteredDirectory &&
                          filteredDirectory.length > 0 &&
                          filteredDirectory.length === selectedItems.length
                        }
                        onCheckedChange={(checked) => {
                          if (checked && filteredDirectory) {
                            setSelectedItems(filteredDirectory.map((item: FileSystemEntity) => item.path));
                          } else {
                            setSelectedItems([]);
                          }
                        }}
                      />
                    </TableHead>
                    <TableHead>Name</TableHead>
                    <TableHead>Size</TableHead>
                    <TableHead>Modified</TableHead>
                    <TableHead className="w-12"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {renderTableContent()}
                </TableBody>
              </Table>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <>
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbLink onClick={() => handlePathChange('')}>Главная</BreadcrumbLink>
              </BreadcrumbItem>
              {pathParts.map((part, index) => (
                <React.Fragment key={part}>
                  <BreadcrumbSeparator>/</BreadcrumbSeparator>
                  <BreadcrumbItem>
                    {index === pathParts.length - 1 ? (
                      <BreadcrumbPage>{part}</BreadcrumbPage>
                    ) : (
                      <BreadcrumbLink
                        onClick={() => handlePathChange(getPathToIndex(index))}
                      >
                        {part}
                      </BreadcrumbLink>
                    )}
                  </BreadcrumbItem>
                </React.Fragment>
              ))}
            </BreadcrumbList>
          </Breadcrumb>

          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              onClick={handleCreateFolderClick}
            >
              Создать папку
            </Button>
            <Button
              variant="outline"
              onClick={handleCreateFileClick}
            >
              Создать файл
            </Button>
            <Input
              type="file"
              className="hidden"
              id="file-upload"
              onChange={handleFileUpload}
            />
            <Button
              variant="outline"
              onClick={() => document.getElementById('file-upload')?.click()}
            >
              Загрузить
            </Button>
            {selectedItems.length > 0 && (
              <>
                <Button
                  variant="secondary"
                  onClick={handleCreateArchiveFromSelected}
                >
                  Создать архив
                </Button>
                <Button
                  variant="destructive"
                  onClick={() => {
                    const itemsToDelete = directory?.filter(item => selectedItems.includes(item.path)) || [];
                    const protectedFiles = itemsToDelete.filter(item => isProtectedFile(item.name));
                    
                    if (protectedFiles.length > 0) {
                      toast.error('Невозможно удалить системные файлы', {
                        description: 'Файл data.db является системным и не может быть удален'
                      });
                      return;
                    }
                    
                    setItemsToDelete(itemsToDelete);
                    setIsDeleteDialogOpen(true);
                  }}
                >
                  Удалить
                </Button>
              </>
            )}
          </div>
        </div>

        <div className="border rounded-md mb-4">
          <Input
            placeholder="Поиск..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full border-0 focus-visible:ring-0 focus-visible:ring-offset-0 rounded-none"
          />
        </div>

        <div className="border rounded-md">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-12">
                  <Checkbox
                    checked={
                      filteredDirectory &&
                      filteredDirectory.length > 0 &&
                      filteredDirectory.length === selectedItems.length
                    }
                    onCheckedChange={(checked) => {
                      if (checked && filteredDirectory) {
                        setSelectedItems(filteredDirectory.map((item) => item.path));
                      } else {
                        setSelectedItems([]);
                      }
                    }}
                  />
                </TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Size</TableHead>
                <TableHead>Modified</TableHead>
                <TableHead className="w-12"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {renderTableContent()}
            </TableBody>
          </Table>
        </div>
      </div>

      <Dialog 
        open={isCreatingFolder} 
        onOpenChange={(open) => {
          if (!open) handleCloseCreateDialog();
          else setIsCreatingFolder(open);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Создать папку</DialogTitle>
          </DialogHeader>
          <div className="space-y-2">
            <Input
              placeholder="Введите имя папки"
              value={newItemName}
              onChange={handleNewItemNameChange}
              className={inputError ? 'border-red-500 focus-visible:ring-red-500' : ''}
            />
            {inputError && (
              <p className="text-sm text-red-500">{inputError}</p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={handleCloseCreateDialog}>
              Отмена
            </Button>
            <Button 
              onClick={() => handleCreateItem('directory')}
              disabled={!newItemName || isItemExists(newItemName)}
            >
              Создать
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog 
        open={isCreatingFile} 
        onOpenChange={(open) => {
          if (!open) handleCloseCreateDialog();
          else setIsCreatingFile(open);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Создать файл</DialogTitle>
          </DialogHeader>
          <div className="space-y-2">
            <Input
              placeholder="Введите имя файла"
              value={newItemName}
              onChange={handleNewItemNameChange}
              className={inputError ? 'border-red-500 focus-visible:ring-red-500' : ''}
            />
            {inputError && (
              <p className="text-sm text-red-500">{inputError}</p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={handleCloseCreateDialog}>
              Отмена
            </Button>
            <Button 
              onClick={() => handleCreateItem('file')}
              disabled={!newItemName || !!getNotEditableReason(newItemName) || isItemExists(newItemName)}
            >
              Создать
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={!!editingFile} onOpenChange={() => setEditingFile(null)}>
        <DialogContent className="sm:max-w-[1000px] sm:h-[800px]">
          <DialogHeader>
            <DialogTitle>Редактирование файла: {editingFile?.path}</DialogTitle>
          </DialogHeader>
          <div className="flex-1 min-h-[600px]">
            {editingFile && (
              <Editor
                height="600px"
                defaultLanguage={getFileLanguage(editingFile.path)}
                value={editingFile.content}
                onChange={handleEditorChange}
                theme="vs-dark"
                options={{
                  minimap: { enabled: true },
                  fontSize: 14,
                  wordWrap: 'on',
                  automaticLayout: true,
                  formatOnPaste: true,
                  formatOnType: true,
                }}
              />
            )}
          </div>
          <DialogFooter>
            <Button type="submit" onClick={handleSaveFile}>
              Сохранить
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Подтверждение удаления</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <p>Вы уверены, что хотите удалить следующие элементы?</p>
            <div className="max-h-[200px] overflow-y-auto space-y-2 border rounded-md p-4">
              {itemsToDelete.map((item) => (
                <div key={item.path} className="flex items-center gap-2">
                  {item.isDirectory ? (
                    <FolderIcon className="h-4 w-4" />
                  ) : (
                    <FileIcon className="h-4 w-4" />
                  )}
                  <span>{item.name}</span>
                </div>
              ))}
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setIsDeleteDialogOpen(false);
                setItemsToDelete([]);
              }}
            >
              Отмена
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteItems}
            >
              Удалить
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
} 