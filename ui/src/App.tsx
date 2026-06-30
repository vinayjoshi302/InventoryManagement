import { useEffect, useMemo, useState } from 'react';

type InventoryItem = {
  sku: string;
  name: string;
  availableQuantity: number;
};

type HoldItem = {
  sku: string;
  quantity: number;
};

type Hold = {
  holdId: string;
  customerId: string;
  status: string;
  expiresAt: string;
  items: HoldItem[];
};

type InventoryResponse = {
  items: InventoryItem[];
};

const API_BASE = 'http://localhost:8080/api';

function App() {
  const [inventory, setInventory] = useState<InventoryItem[]>([]);
  const [holds, setHolds] = useState<Hold[]>([]);
  const [selectedSku, setSelectedSku] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [customerId, setCustomerId] = useState('web-customer');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refreshData = async () => {
    setLoading(true);
    try {
      const [inventoryRes, holdsRes] = await Promise.all([
        fetch(`${API_BASE}/inventory`),
        fetch(`${API_BASE}/holds`),
      ]);
      if (!inventoryRes.ok || !holdsRes.ok) {
        throw new Error('Failed to load inventory and holds');
      }
      const inventoryData: InventoryResponse = await inventoryRes.json();
      const holdsData: Hold[] = await holdsRes.json();
      setInventory(inventoryData.items);
      setHolds(holdsData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void refreshData();
  }, []);

  const handleCreateHold = async (event: React.FormEvent) => {
    event.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_BASE}/holds`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          customerId,
          items: [{ sku: selectedSku, quantity }],
        }),
      });
      if (!response.ok) {
        const data = await response.json();
        throw new Error(data.message ?? 'Unable to place hold');
      }
      await refreshData();
      setSelectedSku('');
      setQuantity(1);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  const releaseHold = async (holdId: string) => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_BASE}/holds/${holdId}`, { method: 'DELETE' });
      if (!response.ok) {
        const data = await response.json();
        throw new Error(data.message ?? 'Unable to release hold');
      }
      await refreshData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  const inventoryOptions = useMemo(() => inventory.filter((item) => item.availableQuantity > 0), [inventory]);

  return (
    <div className="app-shell">
      <h1>Inventory Hold Dashboard</h1>
      <p>Inspect stock, place temporary holds, and release them when checkout completes.</p>
      {error ? <div className="error">{error}</div> : null}
      <section className="card">
        <h2>Inventory</h2>
        <div className="grid">
          {inventory.map((item) => (
            <div key={item.sku} className="inventory-card">
              <strong>{item.name}</strong>
              <div>SKU: {item.sku}</div>
              <div>Available: {item.availableQuantity}</div>
            </div>
          ))}
        </div>
      </section>

      <section className="card">
        <h2>Create Hold</h2>
        <form onSubmit={handleCreateHold} className="form-grid">
          <label>
            Customer ID
            <input value={customerId} onChange={(e) => setCustomerId(e.target.value)} />
          </label>
          <label>
            Product
            <select value={selectedSku} onChange={(e) => setSelectedSku(e.target.value)} required>
              <option value="">Select a product</option>
              {inventoryOptions.map((item) => (
                <option key={item.sku} value={item.sku}>
                  {item.name} ({item.availableQuantity})
                </option>
              ))}
            </select>
          </label>
          <label>
            Quantity
            <input type="number" min="1" value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} />
          </label>
          <button type="submit" disabled={loading || !selectedSku}>
            {loading ? 'Working...' : 'Place Hold'}
          </button>
        </form>
      </section>

      <section className="card">
        <h2>Active Holds</h2>
        {holds.length === 0 ? <p>No active holds.</p> : null}
        <div className="hold-list">
          {holds.map((hold) => (
            <div key={hold.holdId} className="hold-card">
              <div className="hold-header">
                <strong>{hold.holdId}</strong>
                <span>{hold.status}</span>
              </div>
              <div>Customer: {hold.customerId}</div>
              <div>Items: {hold.items.map((item) => `${item.sku} x${item.quantity}`).join(', ')}</div>
              <div>Expires: {new Date(hold.expiresAt).toLocaleString()}</div>
              <button onClick={() => void releaseHold(hold.holdId)} disabled={loading}>
                Release Hold
              </button>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}

export default App;
