export const PARTNER_TOKEN_KEY="everycare_partner_token";
export const API_BASE=process.env.NEXT_PUBLIC_API_BASE_URL??"http://localhost:5185";
export function partnerHeaders(){const token=typeof window!=="undefined"?localStorage.getItem(PARTNER_TOKEN_KEY):null;return token?{Authorization:`Bearer ${token}`}:{Authorization:""};}
