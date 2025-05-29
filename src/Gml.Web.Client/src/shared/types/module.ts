import { ComponentType } from 'react';

export interface IModuleMetadata {
  name?: string;
  description?: string;
  version?: string;
  author?: string;
}

export interface IModuleExports {
  default: ComponentType<any>;
  metadata?: IModuleMetadata;
}

export interface IModule {
  id: string;
  name: string;
  version: string;
  description?: string;
  entry: string;
}

export interface IModuleManifest {
  modules: IModule[];
}

export interface ILoadedModule {
  id: string;
  Component: ComponentType<any>;
  module: IModule;
}

export type ModuleRegistry = Map<string, ILoadedModule>;

// Record для всех возможных модулей
type ModuleRecord = Record<string, IModuleExports>;

declare global {
  interface Window extends ModuleRecord {
    [key: string]: unknown;
  }
} 