// Debug component to test data source switching
import React, { useState } from 'react';
import { getCurrentMode, isApiMode, isMockMode } from '../config/dataSource';
import { useSearchServices } from '../hooks/useServices';

const DataSourceDebug: React.FC = () => {
    const [testParams] = useState({ page: 1, pageSize: 3 });

    // Test the hook
    const { data, isLoading, error } = useSearchServices(testParams);

    // Only show in development
    if (import.meta.env.NODE_ENV === 'production') {
        return null;
    }

    return null;
    // (
    //     <div className="fixed top-4 right-4 z-50 bg-white border border-slate-300 rounded-lg p-4 shadow-lg max-w-sm">
    //         <h3 className="font-bold text-sm mb-2">Data Source Debug</h3>

    //         <div className="text-xs space-y-1 mb-3">
    //             <div>Current Mode: <span className="font-mono">{getCurrentMode()}</span></div>
    //             <div>Is API Mode: <span className="font-mono">{isApiMode().toString()}</span></div>
    //             <div>Is Mock Mode: <span className="font-mono">{isMockMode().toString()}</span></div>
    //         </div>

    //         <div className="text-xs space-y-1">
    //             <div>Hook Status: {isLoading ? 'Loading...' : error ? 'Error' : 'Loaded'}</div>
    //             <div>Services Count: {data?.services?.length || 0}</div>
    //             {data?.services?.slice(0, 2).map(service => (
    //                 <div key={service.id} className="text-xs text-slate-600">
    //                     • {service.name} - {service.price}
    //                 </div>
    //             ))}
    //         </div>
    //     </div>
    // );
};

export default DataSourceDebug;