#!/bin/bash

echo "Installing LocalStack CLI tools..."

# Check if Python/pip is available
if command -v pip3 &> /dev/null; then
    echo "Installing awscli-local using pip3..."
    pip3 install awscli-local
elif command -v pip &> /dev/null; then
    echo "Installing awscli-local using pip..."
    pip install awscli-local
else
    echo "Python/pip not found. Please install Python first."
    echo "Alternative: Use the AWS CLI directly (see alternative script)"
    exit 1
fi

# Verify installation
if command -v awslocal &> /dev/null; then
    echo "✅ awslocal installed successfully!"
    awslocal --version
else
    echo "❌ Installation failed. Please try manual installation:"
    echo "pip install awscli-local"
fi