import fs from 'fs/promises';
import path from 'path';

import { NextResponse } from 'next/server';

export async function GET() {
  try {
    const modulesPath = path.join(process.cwd(), 'public', 'modules');
    const entries = await fs.readdir(modulesPath, { withFileTypes: true });
    
    // Фильтруем только директории и исключаем служебные файлы
    const modules = entries
      .filter(entry => entry.isDirectory() && !entry.name.startsWith('.'))
      .map(entry => entry.name);

    return NextResponse.json(modules);
  } catch (error) {
    console.error('Error scanning modules directory:', error);
    return NextResponse.json({ error: 'Failed to scan modules' }, { status: 500 });
  }
} 