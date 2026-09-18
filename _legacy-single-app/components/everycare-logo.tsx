import Link from "next/link";

export function EveryCareLogo({light=false,compact=false}:{light?:boolean;compact?:boolean}){
 return <Link href="/" className={`ec-logo${light?" light":""}`} aria-label="EveryCare - Trang chủ"><span className="ec-logo-mark"><svg viewBox="0 0 48 48" role="img" aria-hidden="true"><path d="M24 39C17 34.6 8 27.7 8 18.6 8 12.8 12.1 9 17.1 9c3.2 0 5.7 1.6 6.9 4 1.2-2.4 3.7-4 6.9-4 5 0 9.1 3.8 9.1 9.6C40 27.7 31 34.6 24 39Z"/><path className="ec-logo-cross" d="M24 17v12M18 23h12"/></svg></span>{!compact&&<span className="ec-logo-word">Every<span>Care</span></span>}</Link>;
}
