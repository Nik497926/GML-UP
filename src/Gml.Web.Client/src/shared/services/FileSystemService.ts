import { AxiosResponse } from 'axios';

import { $api } from '@/services/api.service';
import {
  TDeleteDirectoriesRequest,
  TDeleteDirectoriesResponse,
  TDeleteDirectoryRequest,
  TDeleteDirectoryResponse,
  TDeleteFileRequest,
  TDeleteFileResponse,
  TDeleteFilesRequest,
  TDeleteFilesResponse,
  TGetDirectoryRequest,
  TGetDirectoryResponse,
  TGetFileContentRequest,
  TGetFileContentResponse,
  TPostArchiveRequest,
  TPostArchiveResponse,
  TPostExtractRequest,
  TPostExtractResponse,
  TPostFileContentRequest,
  TPostFileContentResponse,
  TPostUploadRequest,
  TPostUploadResponse,
  FileSystemEntity
} from '@/shared/api/contracts/filesystem/requests';

class FileSystemService {
  private BASE_URL = '/filesystem';

  async getRootDirectory(): Promise<AxiosResponse<TGetDirectoryResponse>> {
    return await $api.get<TGetDirectoryResponse>(`${this.BASE_URL}/directory`);
  }

  async getDirectory(path: string): Promise<AxiosResponse<TGetDirectoryResponse>> {
    if (!path) {
      return this.getRootDirectory();
    }
    return await $api.get<TGetDirectoryResponse>(`${this.BASE_URL}/directory/${path}`);
  }

  async deleteDirectory(path: string): Promise<AxiosResponse<TDeleteDirectoryResponse>> {
    return await $api.delete<TDeleteDirectoryResponse>(`${this.BASE_URL}/directory/${path}`);
  }

  async createArchive(path: string): Promise<AxiosResponse<TPostArchiveResponse>> {
    return await $api.post<TPostArchiveResponse>(`${this.BASE_URL}/archive/${path}`);
  }

  async createArchiveMultiple(paths: string[]): Promise<AxiosResponse<TPostArchiveResponse>> {
    return await $api.post<TPostArchiveResponse>(`${this.BASE_URL}/archive/multiple`, { paths });
  }

  async extractArchive(archivePath: string): Promise<AxiosResponse<TPostExtractResponse>> {
    return await $api.post<TPostExtractResponse>(`${this.BASE_URL}/extract`, { archivePath });
  }

  async downloadFile(path: string): Promise<Blob> {
    const response = await $api.get(`${this.BASE_URL}/download/${path}`, {
      responseType: 'blob',
    });
    return response.data;
  }

  async uploadFile(path: string, file: File): Promise<AxiosResponse<TPostUploadResponse>> {
    const formData = new FormData();
    formData.append('file', file);
    return await $api.post<TPostUploadResponse>(`${this.BASE_URL}/upload/${path}`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }

  async deleteFiles(paths: string[]): Promise<AxiosResponse<TDeleteFilesResponse>> {
    return await $api.delete<TDeleteFilesResponse>(`${this.BASE_URL}/files`, {
      data: { paths },
    });
  }

  async deleteDirectories(paths: string[]): Promise<AxiosResponse<TDeleteDirectoriesResponse>> {
    return await $api.delete<TDeleteDirectoriesResponse>(`${this.BASE_URL}/directories`, {
      data: { paths },
    });
  }

  async deleteFile(path: string): Promise<AxiosResponse<TDeleteFileResponse>> {
    return await $api.delete<TDeleteFileResponse>(`${this.BASE_URL}/file/${path}`);
  }

  async getFileContent(path: string): Promise<AxiosResponse<TGetFileContentResponse>> {
    return await $api.get<TGetFileContentResponse>(`${this.BASE_URL}/file/${path}`);
  }

  async saveFileContent(path: string, content: string): Promise<AxiosResponse<TPostFileContentResponse>> {
    return await $api.post<TPostFileContentResponse>(`${this.BASE_URL}/file`, {
      path,
      content,
    });
  }

  async createDirectory(path: string): Promise<AxiosResponse<TPostArchiveResponse>> {
    return await $api.post<TPostArchiveResponse>(`${this.BASE_URL}/directory/create/${path}`);
  }

  async createFile(path: string): Promise<AxiosResponse<TPostFileContentResponse>> {
    return await $api.post<TPostFileContentResponse>(`${this.BASE_URL}/file/create/${path}`);
  }
}

export const fileSystemService = new FileSystemService(); 