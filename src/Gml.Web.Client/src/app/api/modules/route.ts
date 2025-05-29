import fs from 'fs/promises';
import path from 'path';

import { NextResponse } from 'next/server';

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  
  if (!searchParams.get('scan')) {
    return NextResponse.json({ error: 'Invalid request' }, { status: 400 });
  }

  try {
    const modulesPath = path.join(process.cwd(), 'public', 'modules');
    const entries = await fs.readdir(modulesPath, { withFileTypes: true });
    
    const modules = entries
      .filter(entry => entry.isDirectory())
      .map(dir => dir.name);

    return NextResponse.json(modules);
  } catch (error) {
    console.error('Error scanning modules directory:', error);
    return NextResponse.json(
      { error: 'Failed to scan modules directory' },
      { status: 500 }
    );
  }
} 