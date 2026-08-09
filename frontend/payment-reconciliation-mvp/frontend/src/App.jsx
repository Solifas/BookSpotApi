import { useMemo, useState } from 'react';

const API_BASE = 'http://localhost:5000/api';

function App() {
  const [bankFile, setBankFile] = useState(null);
  const [systemFile, setSystemFile] = useState(null);
  const [uploadStatus, setUploadStatus] = useState({});
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const canReconcile = useMemo(() => !!bankFile && !!systemFile && !busy, [bankFile, systemFile, busy]);

  const uploadFile = async (source, file) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch(`${API_BASE}/upload/${source}`, {
      method: 'POST',
      body: formData
    });

    const payload = await response.json();
    if (!response.ok) {
      throw new Error(payload.error || `Failed to upload ${source} file.`);
    }

    return payload;
  };

  const handleReconcile = async () => {
    setError('');
    setResult(null);

    if (!bankFile || !systemFile) {
      setError('Please select both bank and system CSV files.');
      return;
    }

    setBusy(true);
    try {
      const bankUpload = await uploadFile('bank', bankFile);
      const systemUpload = await uploadFile('system', systemFile);
      setUploadStatus({ bank: bankUpload, system: systemUpload });

      const reconcileResponse = await fetch(`${API_BASE}/reconcile`, { method: 'POST' });
      const reconcilePayload = await reconcileResponse.json();
      if (!reconcileResponse.ok) {
        throw new Error(reconcilePayload.error || 'Reconciliation failed.');
      }

      setResult(reconcilePayload);
    } catch (uploadError) {
      setError(uploadError.message);
    } finally {
      setBusy(false);
    }
  };

  const exportResults = () => {
    if (!result) return;

    const rows = [
      'Section,Date,Amount,Reference,Description,Reason,CounterpartyDate,CounterpartyAmount,CounterpartyReference'
    ];

    result.matched.forEach((match) => {
      rows.push([
        'Matched',
        match.bankTransaction.date,
        match.bankTransaction.amount,
        quote(match.bankTransaction.reference),
        quote(match.bankTransaction.description),
        quote(match.matchReason),
        match.systemTransaction.date,
        match.systemTransaction.amount,
        quote(match.systemTransaction.reference)
      ].join(','));
    });

    result.missingInBank.forEach((item) => {
      rows.push([
        'MissingInBank',
        item.transaction.date,
        item.transaction.amount,
        quote(item.transaction.reference),
        quote(item.transaction.description),
        quote(item.reason),
        '',
        '',
        ''
      ].join(','));
    });

    result.missingInSystem.forEach((item) => {
      rows.push([
        'MissingInSystem',
        item.transaction.date,
        item.transaction.amount,
        quote(item.transaction.reference),
        quote(item.transaction.description),
        quote(item.reason),
        '',
        '',
        ''
      ].join(','));
    });

    const blob = new Blob([rows.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = 'reconciliation-result.csv';
    link.click();
    URL.revokeObjectURL(link.href);
  };

  return (
    <main className="page">
      <section className="card">
        <h1>Payment Reconciliation</h1>
        <p className="subtitle">Upload bank and system CSV files, then reconcile in one click.</p>

        <div className="upload-grid">
          <label>
            <span>Bank statement CSV</span>
            <input type="file" accept=".csv" onChange={(e) => setBankFile(e.target.files?.[0] || null)} />
          </label>

          <label>
            <span>System transactions CSV</span>
            <input type="file" accept=".csv" onChange={(e) => setSystemFile(e.target.files?.[0] || null)} />
          </label>
        </div>

        <div className="actions">
          <button onClick={handleReconcile} disabled={!canReconcile}>
            {busy ? 'Reconciling…' : 'Reconcile'}
          </button>
          <button onClick={exportResults} disabled={!result}>Export CSV</button>
        </div>

        {uploadStatus.bank && (
          <p className="status">Bank: {uploadStatus.bank.acceptedRows} rows accepted, {uploadStatus.bank.parseErrorCount} parse errors.</p>
        )}

        {uploadStatus.system && (
          <p className="status">System: {uploadStatus.system.acceptedRows} rows accepted, {uploadStatus.system.parseErrorCount} parse errors.</p>
        )}

        {error && <p className="error">{error}</p>}
      </section>

      {result && (
        <section className="results">
          <ResultTable title="Matched transactions" rows={result.matched} type="matched" />
          <ResultTable title="Missing in bank" rows={result.missingInBank} type="unmatched" />
          <ResultTable title="Missing in system" rows={result.missingInSystem} type="unmatched" />

          {result.parseErrors?.length > 0 && (
            <div className="card">
              <h2>Parse errors</h2>
              <ul>
                {result.parseErrors.map((parseError, index) => (
                  <li key={`${parseError.rowNumber}-${index}`}>
                    Row {parseError.rowNumber}: {parseError.message}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </section>
      )}
    </main>
  );
}

function ResultTable({ title, rows, type }) {
  return (
    <div className="card">
      <h2>{title}</h2>
      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th>Amount</th>
            <th>Reference</th>
            <th>Description</th>
            <th>Reason</th>
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 && (
            <tr>
              <td colSpan={5} className="empty">No rows</td>
            </tr>
          )}
          {rows.map((row, idx) => {
            const tx = type === 'matched' ? row.bankTransaction : row.transaction;
            const reason = type === 'matched' ? row.matchReason : row.reason;

            return (
              <tr key={idx}>
                <td>{tx.date}</td>
                <td>{tx.amount}</td>
                <td>{tx.reference || '-'}</td>
                <td>{tx.description || '-'}</td>
                <td>{reason}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function quote(value) {
  if (value == null) return '';
  return `"${String(value).replaceAll('"', '""')}"`;
}

export default App;
