import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import axios from 'axios';

import { fileSystemService } from '@/shared/services/FileSystemService';
import { isAxiosError } from '@/shared/lib/utils';
import { 
  TPostArchiveRequest,
  TDeleteDirectoryRequest,
  TDeleteFileRequest,
  TDeleteFilesRequest,
  TPostExtractRequest,
  TGetDirectoryRequest,
  TGetFileContentRequest,
  TPostFileContentRequest,
  TPostUploadRequest,
  FileSystemEntity
} from '@/shared/api/contracts/filesystem/requests';

export const fileSystemKeys = {
  all: ['filesystem'] as const,
  directory: (path: string) => [...fileSystemKeys.all, 'directory', path] as const,
};

export const useDirectory = (path: string = '') => {
  return useQuery({
    queryKey: fileSystemKeys.directory(path),
    queryFn: () => fileSystemService.getDirectory(path),
    select: (response) => response.data.data,
  });
};

export const useDeleteDirectory = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (path: string) => fileSystemService.deleteDirectory(path),
    onSuccess: async (_, path) => {
      await queryClient.invalidateQueries({ queryKey: fileSystemKeys.directory(path) });
      toast.success('Директория успешно удалена');
    },
    onError: (error) => {
      isAxiosError({ toast, error });
    },
  });
};

export const useCreateArchive = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: string | { type: 'multiple'; paths: string[] }) => {
      if (typeof request === 'string') {
        return await fileSystemService.createArchive(request);
      } else {
        return await fileSystemService.createArchiveMultiple(request.paths);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['directory'] });
    },
  });
};

export const useExtractArchive = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (archivePath: string) => fileSystemService.extractArchive(archivePath),
    onSuccess: async (_, archivePath) => {
      const directory = archivePath.split('/').slice(0, -1).join('/');
      await queryClient.invalidateQueries({ queryKey: fileSystemKeys.directory(directory) });
      toast.success('Архив успешно распакован');
    },
    onError: (error) => {
      isAxiosError({ toast, error });
    },
  });
};

export const useDownloadFile = () => {
  return useMutation({
    mutationFn: async (path: string) => {
      const blob = await fileSystemService.downloadFile(path);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', path.split('/').pop() || 'download');
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    },
    onError: (error) => {
      isAxiosError({ toast, error });
    },
  });
};

export const useUploadFile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ path, file }: { path: string; file: File }) =>
      fileSystemService.uploadFile(path, file),
    onSuccess: async (_, { path }) => {
      await queryClient.invalidateQueries({ queryKey: fileSystemKeys.directory(path) });
      toast.success('Файл успешно загружен');
    },
    onError: (error) => {
      isAxiosError({ toast, error });
    },
  });
};

export const useDeleteFiles = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (paths: string[]) => fileSystemService.deleteFiles(paths),
    onSuccess: async (_, paths) => {
      // Инвалидируем все затронутые директории
      const directories = Array.from(new Set(paths.map((path) => path.split('/').slice(0, -1).join('/'))));
      await Promise.all(
        directories.map((dir) =>
          queryClient.invalidateQueries({ queryKey: fileSystemKeys.directory(dir) }),
        ),
      );
      toast.success('Файлы успешно удалены');
    },
    onError: (error) => {
      isAxiosError({ toast, error });
    },
  });
};

export const useDeleteFile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (path: string) => fileSystemService.deleteFile(path),
    onSuccess: async (_, path) => {
      const directory = path.split('/').slice(0, -1).join('/');
      await queryClient.invalidateQueries({ queryKey: fileSystemKeys.directory(directory) });
      toast.success('Файл успешно удален');
    },
    onError: (error) => {
      isAxiosError({ toast, error });
    },
  });
};

export const useFileContent = () => {
  return useMutation({
    mutationFn: async (path: string) => {
      return await fileSystemService.getFileContent(path);
    },
  });
};

export const useSaveFileContent = () => {
  return useMutation({
    mutationFn: async ({ path, content }: { path: string; content: string }) => {
      return await fileSystemService.saveFileContent(path, content);
    },
  });
};

export const useCreateDirectory = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (path: string) => fileSystemService.createDirectory(path),
    onSuccess: async (_, path) => {
      const directory = path.split('/').slice(0, -1).join('/');
      await queryClient.invalidateQueries({ queryKey: fileSystemKeys.directory(directory) });
      toast.success('Папка успешно создана');
    },
    onError: (error) => {
      isAxiosError({ toast, error });
    },
  });
};

export const useCreateFile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (path: string) => fileSystemService.createFile(path),
    onSuccess: async (_, path) => {
      const directory = path.split('/').slice(0, -1).join('/');
      await queryClient.invalidateQueries({ queryKey: fileSystemKeys.directory(directory) });
      toast.success('Файл успешно создан');
    },
    onError: (error) => {
      isAxiosError({ toast, error });
    },
  });
}; 