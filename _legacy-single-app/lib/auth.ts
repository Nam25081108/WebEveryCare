export type Customer={id:string;fullName:string;phone:string;email?:string;createdAt:string};
export type StoredCustomer=Customer&{passwordHash:string};
export type RegisterInput={fullName:string;phone:string;email?:string;password:string};

const USERS_KEY="everycare_demo_customers";const SESSION_KEY="everycare_customer_session";const API_BASE=process.env.NEXT_PUBLIC_API_BASE_URL??"http://localhost:5185";
const canUseBrowserStorage=()=>typeof window!=="undefined";
export const normalizePhone=(phone:string)=>phone.replace(/\D/g,"");
export const isValidVietnamPhone=(phone:string)=>/^0[35789]\d{8}$/.test(normalizePhone(phone));
export const isValidEmail=(email:string)=>!email||/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
export const validatePassword=(password:string)=>password.length>=8&&/[a-z]/.test(password)&&/[A-Z]/.test(password)&&/\d/.test(password);
async function hashPassword(password:string){const bytes=new TextEncoder().encode(password);const hash=await crypto.subtle.digest("SHA-256",bytes);return Array.from(new Uint8Array(hash)).map(byte=>byte.toString(16).padStart(2,"0")).join("")}
function readCustomers():StoredCustomer[]{if(!canUseBrowserStorage())return[];try{return JSON.parse(localStorage.getItem(USERS_KEY)??"[]") as StoredCustomer[]}catch{return[]}}

async function readResponse(response:Response){const data=await response.json();if(!response.ok)throw new Error(data.message??"Không thể xử lý yêu cầu.");return data;}

export async function registerCustomer(input:RegisterInput):Promise<Customer>{
 try{const response=await fetch(`${API_BASE}/api/customers/register`,{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify(input)});return await readResponse(response) as Customer}
 catch(error){if(error instanceof TypeError)throw new Error("Không thể kết nối backend. Hãy kiểm tra API ở cổng 5185.");throw error}
}

export async function loginCustomer(phoneInput:string,password:string):Promise<Customer>{
 const phone=normalizePhone(phoneInput);
 try{
  const response=await fetch(`${API_BASE}/api/customers/login`,{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({phone,password})});
  if(response.ok)return await response.json() as Customer;
  const data=await response.json();const passwordHash=await hashPassword(password);const legacy=readCustomers().find(item=>item.phone===phone&&item.passwordHash===passwordHash);
  if(!legacy)throw new Error(data.message??"Số điện thoại hoặc mật khẩu chưa chính xác.");
  return await registerCustomer({fullName:legacy.fullName,phone:legacy.phone,email:legacy.email,password});
 }catch(error){if(error instanceof TypeError)throw new Error("Không thể kết nối backend. Hãy kiểm tra API ở cổng 5185.");throw error}
}

export async function syncLocalCustomer(customer:Customer){try{await fetch(`${API_BASE}/api/customers/sync-local`,{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify(customer)})}catch{/* Thử lại ở lần tải trang sau. */}}
export function saveSession(customer:Customer,persistent:boolean){if(!canUseBrowserStorage())return;localStorage.removeItem(SESSION_KEY);sessionStorage.removeItem(SESSION_KEY);(persistent?localStorage:sessionStorage).setItem(SESSION_KEY,JSON.stringify(customer))}
export function loadSession():Customer|null{if(!canUseBrowserStorage())return null;try{const raw=localStorage.getItem(SESSION_KEY)??sessionStorage.getItem(SESSION_KEY);return raw?JSON.parse(raw) as Customer:null}catch{return null}}
export function clearSession(){if(!canUseBrowserStorage())return;localStorage.removeItem(SESSION_KEY);sessionStorage.removeItem(SESSION_KEY)}
