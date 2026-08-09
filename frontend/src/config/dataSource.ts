// Data source configuration - Switch between API and Mock data
export type DataSourceMode = 'api' | 'mock';

// Get initial mode from environment or localStorage (for runtime switching)
const getInitialMode = (): DataSourceMode => {
    // Check localStorage first (for runtime switching)
    if (typeof window !== 'undefined') {
        const storedMode = localStorage.getItem('dataSourceMode') as DataSourceMode;
        if (storedMode === 'api' || storedMode === 'mock') {
            return storedMode;
        }
    }

    // Fall back to environment variable
    const envMode = import.meta.env.VITE_DATA_SOURCE_MODE as DataSourceMode;
    return envMode === 'api' || envMode === 'mock' ? envMode : 'mock';
};

// Configuration object with dynamic mode
export const dataSourceConfig = {
    // API configuration
    api: {
        baseUrl: 'http://localhost:5000',
        timeout: 10000,
    },

    // Mock data configuration
    mock: {
        // Simulate API delays for realistic testing
        simulateDelay: true,
        delayMs: 500,
        // Simulate occasional errors for error handling testing
        simulateErrors: false,
        errorRate: 0.1, // 10% error rate when enabled
    }
};

// Current mode state
let currentMode: DataSourceMode = getInitialMode();

// Helper functions that check current mode dynamically
export const getCurrentMode = (): DataSourceMode => currentMode;
export const isApiMode = () => getCurrentMode() === 'api';
export const isMockMode = () => getCurrentMode() === 'mock';

// Environment variable helper
export const getDataSourceMode = (): DataSourceMode => {
    const envMode = import.meta.env.VITE_DATA_SOURCE_MODE;
    return envMode === 'api' || envMode === 'mock' ? envMode : 'mock';
};

// Runtime mode switching (for development/testing)
export const setDataSourceMode = (mode: DataSourceMode) => {
    console.log(`🔄 Setting data source mode from ${currentMode} to ${mode}`);
    currentMode = mode;

    // Persist to localStorage for runtime switching
    if (typeof window !== 'undefined') {
        localStorage.setItem('dataSourceMode', mode);
        console.log(`💾 Saved to localStorage: ${mode}`);
    }

    console.log(`✅ Data source switched to: ${mode.toUpperCase()}`);
    console.log(`🔍 getCurrentMode() now returns: ${getCurrentMode()}`);
};