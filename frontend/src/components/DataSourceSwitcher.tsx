// Data Source Switcher - Development tool to switch between API and Mock data
import React, { useState, useEffect } from 'react';
import { getCurrentMode, setDataSourceMode, DataSourceMode, isApiMode, isMockMode } from '../config/dataSource';
import { Database, Globe, TestTube } from 'lucide-react';

const DataSourceSwitcher: React.FC = () => {
    const [currentMode, setCurrentMode] = useState<DataSourceMode>(getCurrentMode());
    const [isSwitching, setIsSwitching] = useState(false);

    const handleModeChange = (mode: DataSourceMode) => {
        if (mode === currentMode) return; // Don't switch if already in that mode

        setIsSwitching(true);
        setDataSourceMode(mode);
        setCurrentMode(mode);

        // Show a brief confirmation before reloading
        console.log(`🔄 Switching to ${mode.toUpperCase()} mode...`);

        // Small delay to show the UI update, then reload
        setTimeout(() => {
            window.location.reload();
        }, 300);
    };
    // Only show in development
    // if (import.meta.env.NODE_ENV === 'mock') {
    // }

    return null;
    return (
        <div className="fixed bottom-4 right-4 z-50">
            <div className="bg-white rounded-lg shadow-lg border border-slate-200 p-4 min-w-[200px]">
                <div className="flex items-center space-x-2 mb-3">
                    <Database className="h-4 w-4 text-slate-600" />
                    <span className="text-sm font-medium text-slate-700">Data Source</span>
                </div>

                <div className="space-y-2">
                    <button
                        onClick={() => handleModeChange('mock')}
                        disabled={isSwitching}
                        className={`w-full flex items-center space-x-2 px-3 py-2 rounded-md text-sm transition-colors ${isSwitching
                            ? 'bg-slate-100 text-slate-400 cursor-not-allowed'
                            : currentMode === 'mock'
                                ? 'bg-blue-100 text-blue-700 border border-blue-300'
                                : 'bg-slate-50 text-slate-600 hover:bg-slate-100'
                            }`}
                    >
                        <TestTube className="h-4 w-4" />
                        <span>Mock Data</span>
                        {currentMode === 'mock' && (
                            <span className="ml-auto text-xs bg-blue-200 text-blue-800 px-2 py-1 rounded">
                                Active
                            </span>
                        )}
                    </button>

                    <button
                        onClick={() => handleModeChange('api')}
                        disabled={isSwitching}
                        className={`w-full flex items-center space-x-2 px-3 py-2 rounded-md text-sm transition-colors ${isSwitching
                            ? 'bg-slate-100 text-slate-400 cursor-not-allowed'
                            : currentMode === 'api'
                                ? 'bg-green-100 text-green-700 border border-green-300'
                                : 'bg-slate-50 text-slate-600 hover:bg-slate-100'
                            }`}
                    >
                        <Globe className="h-4 w-4" />
                        <span>Live API</span>
                        {currentMode === 'api' && (
                            <span className="ml-auto text-xs bg-green-200 text-green-800 px-2 py-1 rounded">
                                Active
                            </span>
                        )}
                    </button>
                </div>

                <div className="mt-3 pt-3 border-t border-slate-200">
                    <p className="text-xs text-slate-500">
                        Current: <span className="font-medium">{currentMode.toUpperCase()}</span>
                    </p>
                    {isSwitching ? (
                        <p className="text-xs text-blue-600 mt-1 animate-pulse">
                            Switching... Please wait
                        </p>
                    ) : (
                        <p className="text-xs text-slate-400 mt-1">
                            Switch will reload the page
                        </p>
                    )}

                    <button
                        onClick={() => {
                            console.log('🔍 Current mode check:', getCurrentMode());
                            console.log('🔍 isApiMode():', isApiMode());
                            console.log('🔍 isMockMode():', isMockMode());
                        }}
                        className="w-full mt-2 px-2 py-1 text-xs bg-slate-100 hover:bg-slate-200 rounded text-slate-600"
                    >
                        Test Current Mode
                    </button>
                </div>
            </div>
        </div>
    );
};

export default DataSourceSwitcher;