import fs from 'fs/promises';
import path from 'path';

import { NextResponse } from 'next/server';

export async function GET(
  request: Request,
  { params }: { params: { moduleId: string } }
) {
  try {
    const modulePath = path.join(process.cwd(), 'public', 'modules', params.moduleId, 'index.js');
    const content = await fs.readFile(modulePath, 'utf-8');
    
    return new NextResponse(content, {
      headers: {
        'Content-Type': 'application/javascript',
        'Cache-Control': 'no-store, must-revalidate',
      },
    });
  } catch (error) {
    console.error(`Error loading module ${params.moduleId}:`, error);
    return NextResponse.json(
      { error: `Failed to load module ${params.moduleId}` },
      { status: 500 }
    );
  }
} 