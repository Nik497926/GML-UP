'use client';

import React, { useEffect, useState } from 'react';

import { moduleService } from '../services/ModuleService';
import { ILoadedModule } from '../types/module';

export const ModuleLoader: React.FC = () => {
  const [modules, setModules] = useState<ILoadedModule[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadModules = async () => {
      try {
        await moduleService.loadAllModules();
        const loadedModules = moduleService.getAllLoadedModules();
        setModules(loadedModules);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load modules');
      } finally {
        setLoading(false);
      }
    };

    loadModules();
  }, []);

  if (loading) {
    return <div>Loading modules...</div>;
  }

  if (error) {
    return <div>Error: {error}</div>;
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 p-4">
      {modules.map((moduleItem) => {
        const { Component, module } = moduleItem;
        return (
          <div key={module.id} className="border rounded-lg p-4 shadow-sm">
            <h3 className="text-lg font-semibold mb-2">{module.name}</h3>
            {module.description && (
              <p className="text-gray-600 mb-4">{module.description}</p>
            )}
            <div className="bg-gray-50 p-4 rounded">
              <Component />
            </div>
          </div>
        );
      })}
    </div>
  );
}; 