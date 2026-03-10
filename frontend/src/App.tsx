import { useState, useEffect } from 'react';
import { PasskeyPaymentSDK } from 'passkey-sdk';
import './index.css';

function App() {
  const [pan, setPan] = useState('');
  const [amount, setAmount] = useState('100.00');
  const [status, setStatus] = useState({ type: '', message: '' });
  const [loading, setLoading] = useState(false);
  const [sdk, setSdk] = useState<PasskeyPaymentSDK | null>(null);

  useEffect(() => {
    // VITE_API_URL is injected by .NET Aspire
    const backendUrl = import.meta.env.VITE_API_URL || 'https://localhost:5001';
    const initSdk = new PasskeyPaymentSDK({ backendUrl });
    setSdk(initSdk);
  }, []);

  const handleRegister = async () => {
    if (!sdk || !pan) {
        setStatus({ type: 'error', message: 'Please enter a card number to register.'});
        return;
    }
    
    setLoading(true);
    setStatus({ type: 'info', message: 'Prompting for Passkey registration...' });
    
    try {
      const result = await sdk.bindCard({ pan });
      setStatus({ type: 'success', message: 'Card successfully registered with Passkey!' });
    } catch (err: any) {
      setStatus({ type: 'error', message: err.message || 'Registration failed.' });
    } finally {
      setLoading(false);
    }
  };

  const handlePayment = async () => {
    if (!sdk || !pan || !amount) {
        setStatus({ type: 'error', message: 'Please enter a card number and amount.'});
        return;
    }

    setLoading(true);
    setStatus({ type: 'info', message: 'Authenticating payment with Passkey...' });

    try {
      const result = await sdk.authenticatePayment({ pan, amount: parseFloat(amount) });
      setStatus({ type: 'success', message: `Payment Approved! (TxID: ${result.transactionId})` });
    } catch (err: any) {
      setStatus({ type: 'error', message: err.message || 'Payment authentication failed. Fallback to standard 3DS required.' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app-container">
      <div className="checkout-card">
        <div className="header">
          <h1>Secure Checkout</h1>
          <p>Mock Environment for Provider-Based Passkeys</p>
        </div>

        <div className="form-group">
          <label>Card Number (PAN)</label>
          <input 
            type="text" 
            placeholder="Mock Visa (4..) or Mastercard (5..)" 
            value={pan} 
            onChange={(e) => setPan(e.target.value)} 
          />
        </div>

        <div className="form-group row">
            <div className="col">
                <label>Amount ($)</label>
                <input 
                    type="number" 
                    value={amount} 
                    onChange={(e) => setAmount(e.target.value)} 
                />
            </div>
            <div className="col">
                <label>CVV</label>
                <input type="text" placeholder="123" />
            </div>
        </div>

        {status.message && (
          <div className={`status-message ${status.type}`}>
            {status.message}
          </div>
        )}

        <div className="actions">
          <button 
            className="btn btn-secondary" 
            onClick={handleRegister} 
            disabled={loading}
          >
            {loading ? 'Processing...' : 'Save Card to Passkey'}
          </button>
          
          <button 
            className="btn btn-primary" 
            onClick={handlePayment} 
            disabled={loading}
          >
            {loading ? 'Processing...' : `Pay $${amount} securely`}
          </button>
        </div>
      </div>
    </div>
  );
}

export default App;
