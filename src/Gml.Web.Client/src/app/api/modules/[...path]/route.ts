import { promises as fs } from 'fs';
import path from 'path';

import { NextResponse } from 'next/server';

export async function GET(request: Request, { params }: { params: { path: string[] } }) {
  try {
    // Получаем путь к файлу из параметров
    const filePath = path.join(process.cwd(), 'public', 'modules', ...params.path);

    // Проверяем, что путь не выходит за пределы директории modules
    const normalizedPath = path.normalize(filePath);
    if (!normalizedPath.startsWith(path.join(process.cwd(), 'public', 'modules'))) {
      return new NextResponse('Access denied', { status: 403 });
    }

    // Читаем содержимое файла
    const content = await fs.readFile(filePath, 'utf-8');

    // Определяем тип контента на основе расширения файла
    const ext = path.extname(filePath);
    const contentType = {
      '.js': 'application/javascript',
      '.ts': 'application/javascript',
      '.jsx': 'application/javascript',
      '.tsx': 'application/javascript',
      '.css': 'text/css',
    }[ext] || 'text/plain';

    // Возвращаем содержимое файла с правильным Content-Type
    return new NextResponse(content, {
      headers: {
        'Content-Type': contentType,
        'Cache-Control': 'no-store',
      },
    });
  } catch (error) {
    console.error('Error serving module:', error);
    return new NextResponse('Module not found', { status: 404 });
  }
} 