export interface SDKConfig {
    backendUrl: string;
}

export interface CardData {
    pan: string;
}

export interface PaymentData {
    pan: string;
    amount: number;
}

export class PasskeyPaymentSDK {
    private backendUrl: string;

    constructor(config: SDKConfig) {
        this.backendUrl = config.backendUrl.replace(/\/$/, '');
        console.log(`[PasskeyPaymentSDK] Initialized with backend: ${this.backendUrl}`);
    }

    // Helper functions for WebAuthn ArrayBuffer conversions
    private base64urlToArrayBuffer(base64url: string): ArrayBuffer {
        const padding = '='.repeat((4 - (base64url.length % 4)) % 4);
        const base64 = (base64url + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');
        const rawData = atob(base64);
        const outputArray = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray.buffer;
    }

    private arrayBufferToBase64url(buffer: ArrayBuffer): string {
        let binary = '';
        const bytes = new Uint8Array(buffer);
        const len = bytes.byteLength;
        for (let i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary)
            .replace(/\+/g, '-')
            .replace(/\//g, '_')
            .replace(/=+$/, '');
    }

    async bindCard(cardData: CardData) {
        console.log("[PasskeyPaymentSDK] bindCard started", cardData.pan.substring(0, 4) + '...');
        try {
            // 1. Fetch options from our .NET backend
            const optionsRes = await fetch(`${this.backendUrl}/api/passkeys/register/options`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ pan: cardData.pan })
            });
            if (!optionsRes.ok) throw new Error("Failed to get register options");
            const optionsData = await optionsRes.json();

            // 2. Map options to WebAuthn PublicKeyCredentialCreationOptions
            const publicKey: PublicKeyCredentialCreationOptions = {
                challenge: this.base64urlToArrayBuffer(optionsData.challenge),
                rp: { name: "Passkey Integrator PoC", id: optionsData.rpId },
                user: {
                    id: this.base64urlToArrayBuffer(optionsData.userId),
                    name: "user@example.com",
                    displayName: "Customer"
                },
                pubKeyCredParams: [
                    { type: "public-key", alg: -7 } // ES256
                ],
                authenticatorSelection: {
                    authenticatorAttachment: "platform",
                    userVerification: "required"
                },
                timeout: 60000,
                attestation: "direct"
            };

            // 3. Prompt user for Passkey
            const credential = await navigator.credentials.create({ publicKey }) as PublicKeyCredential;
            if (!credential) throw new Error("Passkey creation was cancelled or failed.");

            const response = credential.response as AuthenticatorAttestationResponse;

            // 4. Send the attestation to the backend (mock verification)
            const verifyRes = await fetch(`${this.backendUrl}/api/passkeys/register/verify`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    pan: cardData.pan,
                    challenge: optionsData.challenge,
                    clientDataJson: this.arrayBufferToBase64url(response.clientDataJSON),
                    attestationObject: this.arrayBufferToBase64url(response.attestationObject)
                })
            });

            if (!verifyRes.ok) throw new Error("Backend failed to verify passkey registration.");
            const verifyData = await verifyRes.json();
            console.log("[PasskeyPaymentSDK] Registered successfully:", verifyData);

            return verifyData;
        } catch (error) {
            console.error("[PasskeyPaymentSDK] Error during bindCard:", error);
            throw error;
        }
    }

    async authenticatePayment(paymentData: PaymentData) {
        console.log("[PasskeyPaymentSDK] authenticatePayment started for $", paymentData.amount);
        try {
            // 1. Fetch challenge from backend
            const optionsRes = await fetch(`${this.backendUrl}/api/passkeys/auth/options`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(paymentData)
            });
            if (!optionsRes.ok) throw new Error("Failed to get auth options");
            const optionsData = await optionsRes.json();

            // 2. Build Authentication Options
            const publicKey: PublicKeyCredentialRequestOptions = {
                challenge: this.base64urlToArrayBuffer(optionsData.challenge),
                rpId: optionsData.rpId,
                userVerification: "required",
                timeout: 60000
            };

            // 3. Prompt user to authenticate
            const assertion = await navigator.credentials.get({ publicKey }) as PublicKeyCredential;
            if (!assertion) throw new Error("Passkey authentication was cancelled or failed.");
            
            const response = assertion.response as AuthenticatorAssertionResponse;

            // 4. Send result back to verify and 'authorize' payment
            const verifyRes = await fetch(`${this.backendUrl}/api/passkeys/auth/verify`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    pan: paymentData.pan,
                    amount: paymentData.amount,
                    challenge: optionsData.challenge,
                    clientDataJson: this.arrayBufferToBase64url(response.clientDataJSON),
                    authenticatorData: this.arrayBufferToBase64url(response.authenticatorData),
                    signature: this.arrayBufferToBase64url(response.signature)
                })
            });

            const verifyData = await verifyRes.json();
            console.log("[PasskeyPaymentSDK] Authenticated payment:", verifyData);

            return verifyData;
        } catch (error) {
            console.error("[PasskeyPaymentSDK] Error during authenticatePayment:", error);
            throw error;
        }
    }
}
