import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly keycloakBaseUrl = 'http://localhost:7890';
  private readonly keycloakRealm = 'toyota';
  private readonly keycloakClientId = 'app-1';
  private readonly scope = 'openid profile email';
  private readonly accessTokenKey = 'app1_access_token';
  private readonly refreshTokenKey = 'app1_refresh_token';
  private readonly idTokenKey = 'app1_id_token';
  private readonly expiresAtKey = 'app1_expires_at';
  private readonly stateKey = 'app1_oidc_state';
  private readonly codeVerifierKey = 'app1_oidc_code_verifier';
  private readonly silentSsoAttemptedKey = 'app1_silent_sso_attempted';

  isAuthenticated(): boolean {
    const token = sessionStorage.getItem(this.accessTokenKey);
    const expiresAt = Number(sessionStorage.getItem(this.expiresAtKey) || '0');
    return Boolean(token) && expiresAt > Date.now() + 5000;
  }

  getUserDisplayName(): string {
    const idToken = sessionStorage.getItem(this.idTokenKey);
    if (!idToken) return 'Unknown';
    const payload = this.parseJwtPayload(idToken);
    return (
      payload['name'] ||
      payload['preferred_username'] ||
      payload['email'] ||
      payload['sub'] ||
      'Unknown'
    );
  }

  hasAttemptedSilentSso(): boolean {
    return sessionStorage.getItem(this.silentSsoAttemptedKey) === '1';
  }

  clearSilentSsoAttempted(): void {
    sessionStorage.removeItem(this.silentSsoAttemptedKey);
  }

  /** Redirect to Keycloak with prompt=none to reuse an existing SSO session. */
  async trySilentSsoLogin(): Promise<boolean> {
    if (this.isAuthenticated()) return false;
    if (this.hasAttemptedSilentSso()) return false;

    sessionStorage.setItem(this.silentSsoAttemptedKey, '1');
    await this.redirectToLogin({ prompt: 'none' });
    return true;
  }

  async redirectToLogin(options?: { prompt?: 'none' | 'login' }): Promise<void> {
    if (options?.prompt !== 'none') {
      this.clearSilentSsoAttempted();
    }

    const state = this.randomString(24);
    const codeVerifier = this.randomString(48);
    const codeChallenge = await this.sha256Base64Url(codeVerifier);

    sessionStorage.setItem(this.stateKey, state);
    sessionStorage.setItem(this.codeVerifierKey, codeVerifier);

    const authUrl = new URL(`${this.keycloakBaseUrl}/realms/${encodeURIComponent(this.keycloakRealm)}/protocol/openid-connect/auth`);
    authUrl.searchParams.set('client_id', this.keycloakClientId);
    authUrl.searchParams.set('redirect_uri', `${window.location.origin}/auth/callback`);
    authUrl.searchParams.set('response_type', 'code');
    authUrl.searchParams.set('scope', this.scope);
    authUrl.searchParams.set('state', state);
    authUrl.searchParams.set('code_challenge', codeChallenge);
    authUrl.searchParams.set('code_challenge_method', 'S256');
    if (options?.prompt) {
      authUrl.searchParams.set('prompt', options.prompt);
    }
    window.location.assign(authUrl.toString());
  }

  async completeLoginFromCallback(queryString: string): Promise<'success' | 'login_required' | 'failed'> {
    const params = new URLSearchParams(queryString.startsWith('?') ? queryString.slice(1) : queryString);
    const error = params.get('error');
    if (error === 'login_required') {
      return 'login_required';
    }
    if (error) {
      return 'failed';
    }

    const code = params.get('code');
    const state = params.get('state');
    const storedState = sessionStorage.getItem(this.stateKey);
    const codeVerifier = sessionStorage.getItem(this.codeVerifierKey);
    if (!code || !state || !storedState || !codeVerifier || state !== storedState) return 'failed';

    const tokenUrl = `${this.keycloakBaseUrl}/realms/${encodeURIComponent(this.keycloakRealm)}/protocol/openid-connect/token`;
    const body = new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: this.keycloakClientId,
      code,
      redirect_uri: `${window.location.origin}/auth/callback`,
      code_verifier: codeVerifier,
    });

    const response = await fetch(tokenUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body,
    });
    if (!response.ok) return 'failed';

    const token = (await response.json()) as {
      access_token: string;
      refresh_token?: string;
      id_token?: string;
      expires_in: number;
    };
    this.persistSession(token);
    this.clearSilentSsoAttempted();
    return 'success';
  }

  async refreshSession(): Promise<boolean> {
    const refreshToken = sessionStorage.getItem(this.refreshTokenKey);
    if (!refreshToken) return false;
    const tokenUrl = `${this.keycloakBaseUrl}/realms/${encodeURIComponent(this.keycloakRealm)}/protocol/openid-connect/token`;
    const body = new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: this.keycloakClientId,
      refresh_token: refreshToken,
    });
    const response = await fetch(tokenUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body,
    });
    if (!response.ok) return false;
    const token = (await response.json()) as {
      access_token: string;
      refresh_token?: string;
      id_token?: string;
      expires_in: number;
    };
    this.persistSession(token);
    return true;
  }

  logout(): void {
    const idTokenHint = sessionStorage.getItem(this.idTokenKey);
    this.clearSession();
    const logoutUrl = new URL(`${this.keycloakBaseUrl}/realms/${encodeURIComponent(this.keycloakRealm)}/protocol/openid-connect/logout`);
    logoutUrl.searchParams.set('client_id', this.keycloakClientId);
    logoutUrl.searchParams.set('post_logout_redirect_uri', `${window.location.origin}/login`);
    if (idTokenHint) logoutUrl.searchParams.set('id_token_hint', idTokenHint);
    window.location.replace(logoutUrl.toString());
  }

  private persistSession(token: { access_token: string; refresh_token?: string; id_token?: string; expires_in: number }): void {
    sessionStorage.setItem(this.accessTokenKey, token.access_token);
    if (token.refresh_token) sessionStorage.setItem(this.refreshTokenKey, token.refresh_token);
    if (token.id_token) sessionStorage.setItem(this.idTokenKey, token.id_token);
    sessionStorage.setItem(this.expiresAtKey, String(Date.now() + token.expires_in * 1000));
  }

  private clearSession(): void {
    sessionStorage.removeItem(this.accessTokenKey);
    sessionStorage.removeItem(this.refreshTokenKey);
    sessionStorage.removeItem(this.idTokenKey);
    sessionStorage.removeItem(this.expiresAtKey);
    sessionStorage.removeItem(this.stateKey);
    sessionStorage.removeItem(this.codeVerifierKey);
    sessionStorage.removeItem(this.silentSsoAttemptedKey);
  }

  private parseJwtPayload(token: string): Record<string, string> {
    try {
      const payload = token.split('.')[1];
      if (!payload) return {};
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const decoded = atob(normalized);
      return JSON.parse(decoded) as Record<string, string>;
    } catch {
      return {};
    }
  }

  private randomString(byteLength: number): string {
    const bytes = new Uint8Array(byteLength);
    crypto.getRandomValues(bytes);
    return this.base64UrlEncode(bytes);
  }

  private async sha256Base64Url(value: string): Promise<string> {
    const data = new TextEncoder().encode(value);
    const digest = await crypto.subtle.digest('SHA-256', data);
    return this.base64UrlEncode(new Uint8Array(digest));
  }

  private base64UrlEncode(bytes: Uint8Array): string {
    const text = btoa(String.fromCharCode(...bytes));
    return text.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  }
}
