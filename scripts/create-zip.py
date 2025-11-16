#!/usr/bin/env python3
"""
Create a proper ZIP file for AWS Lambda deployment
"""
import os
import zipfile
from pathlib import Path

def create_lambda_zip():
    # Paths
    publish_dir = Path("BookSpot.API/publish")
    output_zip = Path("bookspot-api.zip")
    
    # Remove existing zip if it exists
    if output_zip.exists():
        output_zip.unlink()
        print(f"Removed existing {output_zip}")
    
    # Create ZIP file
    print(f"Creating Lambda deployment package...")
    with zipfile.ZipFile(output_zip, 'w', zipfile.ZIP_DEFLATED) as zipf:
        # Walk through all files in publish directory
        for file_path in publish_dir.rglob('*'):
            if file_path.is_file():
                # Add file to zip with relative path
                arcname = file_path.relative_to(publish_dir)
                zipf.write(file_path, arcname)
                print(f"  Added: {arcname}")
    
    # Get file size
    size_mb = output_zip.stat().st_size / (1024 * 1024)
    print(f"\n✅ Lambda package created: {output_zip}")
    print(f"📦 Package size: {size_mb:.2f} MB")
    
    if size_mb > 50:
        print("⚠️  Warning: Package exceeds 50MB. Consider using S3 for deployment.")

if __name__ == "__main__":
    create_lambda_zip()
