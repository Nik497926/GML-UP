import { ResponseBaseEntity } from '@/shared/api/schemas';

// Базовые сущности
export interface FileSystemEntity {
  name: string;
  path: string;
  fullPath: string;
  isDirectory: boolean;
  size?: number;
  modifiedAt?: string;
}

// GET /filesystem/directory/{path}
export type TGetDirectoryRequest = {
  path: string;
};

export type TGetDirectoryResponse = ResponseBaseEntity & {
  data: FileSystemEntity[];
};

// DELETE /filesystem/directory/{path}
export type TDeleteDirectoryRequest = {
  path: string;
};

export type TDeleteDirectoryResponse = ResponseBaseEntity & {
  data: null;
};

// POST /filesystem/archive/{path}
export type TPostArchiveRequest = string | {
  type: 'multiple';
  paths: string[];
};

export type TPostArchiveResponse = ResponseBaseEntity & {
  data: null;
};

// POST /filesystem/extract
export type TPostExtractRequest = {
  archivePath: string;
};

export type TPostExtractResponse = ResponseBaseEntity & {
  data: null;
};

// GET /filesystem/download/{path}
export type TGetDownloadRequest = {
  path: string;
};

// POST /filesystem/upload/{path}
export type TPostUploadRequest = {
  path: string;
  file: File;
};

export type TPostUploadResponse = ResponseBaseEntity & {
  data: null;
};

// DELETE /filesystem/files
export type TDeleteFilesRequest = {
  paths: string[];
};

export type TDeleteFilesResponse = ResponseBaseEntity & {
  data: null;
};

// DELETE /filesystem/directories
export type TDeleteDirectoriesRequest = {
  paths: string[];
};

export type TDeleteDirectoriesResponse = ResponseBaseEntity & {
  data: null;
};

// DELETE /filesystem/file/{path}
export type TDeleteFileRequest = {
  path: string;
};

export type TDeleteFileResponse = ResponseBaseEntity & {
  data: null;
};

// GET /filesystem/file/{path}
export type TGetFileContentRequest = {
  path: string;
};

export type TGetFileContentResponse = ResponseBaseEntity & {
  data: string;
};

// POST /filesystem/file
export type TPostFileContentRequest = {
  path: string;
  content: string;
};

export type TPostFileContentResponse = ResponseBaseEntity & {
  data: null;
};

// POST /filesystem/directory/create/{path}
export type TCreateDirectoryResponse = ResponseBaseEntity & {
  data: null;
};

// POST /filesystem/file/create/{path}
export type TCreateFileResponse = ResponseBaseEntity & {
  data: null;
}; 