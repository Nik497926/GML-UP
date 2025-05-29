import React from 'react';

import { IModule, ILoadedModule, ModuleRegistry, IModuleExports } from '../types/module';

class ModuleService {
  private moduleRegistry: ModuleRegistry = new Map();
  private modulesPath = '/modules';
  private apiPath = '/api/modules';

  private async scanModuleDirectory(): Promise<IModule[]> {
    try {
      const response = await fetch(`${this.apiPath}?scan=true`);
      if (!response.ok) {
        throw new Error('Failed to scan modules directory');
      }
      const directories = await response.json() as string[];
      
      return directories.map(dir => ({
        id: dir,
        name: dir,
        version: '1.0.0',
        entry: 'index.js'
      }));
    } catch (error) {
      console.error('Error scanning modules directory:', error);
      return [];
    }
  }

  private async waitForModuleRegistration(moduleId: string, timeout: number = 5000): Promise<IModuleExports> {
    const startTime = Date.now();
    
    while (Date.now() - startTime < timeout) {
      const moduleExports = (window as any)[moduleId] as IModuleExports;
      if (moduleExports?.default) {
        return moduleExports;
      }
      await new Promise(resolve => setTimeout(resolve, 50));
    }
    
    throw new Error(`Timeout waiting for module ${moduleId} to register`);
  }

  async loadModule(moduleInfo: IModule): Promise<void> {
    try {
      if (this.moduleRegistry.has(moduleInfo.id)) {
        return;
      }

      // Делаем React доступным глобально
      (window as any).React = React;

      const moduleId = `module_${moduleInfo.id}`;

      // Загружаем код модуля через API
      const response = await fetch(`${this.apiPath}/${moduleInfo.id}`);
      if (!response.ok) {
        throw new Error(`Failed to load module ${moduleInfo.id}`);
      }
      const moduleCode = await response.text();

      // Создаем и выполняем скрипт
      const script = document.createElement('script');
      script.type = 'text/javascript';
      script.text = moduleCode;
      document.head.appendChild(script);

      // Ждем регистрации модуля
      const moduleExports = await this.waitForModuleRegistration(moduleId);

      const loadedModule: ILoadedModule = {
        id: moduleInfo.id,
        Component: moduleExports.default,
        module: {
          ...moduleInfo,
          ...(moduleExports.metadata || {})
        }
      };

      this.moduleRegistry.set(moduleInfo.id, loadedModule);
      delete (window as any)[moduleId];
      
      // Удаляем скрипт после загрузки
      document.head.removeChild(script);
    } catch (error) {
      console.error(`Error loading module ${moduleInfo.id}:`, error);
      throw error;
    }
  }

  async loadAllModules(): Promise<void> {
    const modules = await this.scanModuleDirectory();
    await Promise.all(modules.map(module => this.loadModule(module)));
  }

  getLoadedModule(moduleId: string): ILoadedModule | undefined {
    return this.moduleRegistry.get(moduleId);
  }

  getAllLoadedModules(): ILoadedModule[] {
    return Array.from(this.moduleRegistry.values());
  }
}

export const moduleService = new ModuleService(); 