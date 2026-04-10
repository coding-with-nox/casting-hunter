// Keycloak auth — abilitato tramite VITE_KEYCLOAK_ENABLED=true
// Quando disabilitato non importa nulla e tutte le funzioni sono no-op.
// Per abilitare: installare keycloak-js e impostare VITE_KEYCLOAK_ENABLED=true

export const KEYCLOAK_ENABLED = import.meta.env.VITE_KEYCLOAK_ENABLED === 'true';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
let _keycloak: any = null;
let _token: string | undefined;

export async function initAuth(): Promise<void> {
  if (!KEYCLOAK_ENABLED) return;
  // Dynamic import — keycloak-js deve essere installato quando abilitato
  // La variabile impedisce al type-checker di risolvere staticamente il modulo
  const mod = 'keycloak-js';
  // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
  const { default: Keycloak } = await import(/* @vite-ignore */ mod);
  // eslint-disable-next-line @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-assignment
  _keycloak = new Keycloak({
    url: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
    realm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'myrealm',
    clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'actoring-app',
  });
  // eslint-disable-next-line @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
  const authenticated = await _keycloak.init({ onLoad: 'login-required', checkLoginIframe: false });
  // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
  if (!authenticated) { _keycloak.login(); return; }
  // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
  _token = _keycloak.token as string;
}

export function getAuthToken(): string | undefined {
  return KEYCLOAK_ENABLED ? _token : undefined;
}

export async function refreshAuthToken(): Promise<void> {
  if (!KEYCLOAK_ENABLED || !_keycloak) return;
  try {
    // eslint-disable-next-line @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
    const refreshed = await _keycloak.updateToken(30) as boolean;
    // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
    if (refreshed) _token = _keycloak.token as string;
  } catch {
    // eslint-disable-next-line @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
    _keycloak.login();
    throw new Error('Session expired');
  }
}
