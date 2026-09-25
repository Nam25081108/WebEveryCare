"use client";

import { useState } from "react";
import { CircleMarker, MapContainer, TileLayer, useMap, useMapEvents } from "react-leaflet";
import { LocateFixed, MapPin } from "lucide-react";
import type { LatLngExpression } from "leaflet";

export type MapLocation = { latitude: number; longitude: number };
const HO_CHI_MINH_CENTER: LatLngExpression = [10.7769, 106.7009];

function ClickHandler({ onChange }: { onChange:(location:MapLocation)=>void }) {
  useMapEvents({ click(event) { onChange({ latitude:event.latlng.lat, longitude:event.latlng.lng }); } });
  return null;
}

function Recenter({ location }: { location:MapLocation|null }) {
  const map=useMap();
  if(location) map.setView([location.latitude,location.longitude],16);
  return null;
}

export default function PartnerLocationMap({ value, onChange }: { value:MapLocation|null; onChange:(location:MapLocation)=>void }) {
  const [geoError,setGeoError]=useState("");
  function locate(){
    setGeoError("");
    if(!navigator.geolocation){setGeoError("Trình duyệt không hỗ trợ định vị.");return;}
    navigator.geolocation.getCurrentPosition(
      position=>onChange({latitude:position.coords.latitude,longitude:position.coords.longitude}),
      ()=>setGeoError("Không lấy được vị trí. Hãy cho phép quyền vị trí hoặc bấm trực tiếp trên bản đồ."),
      {enableHighAccuracy:true,timeout:12000}
    );
  }
  return <div className="location-picker">
    <div className="location-picker-head"><span><MapPin/><strong>Xác nhận vị trí trên bản đồ</strong><small>Bấm đúng vị trí của địa chỉ bạn vừa nhập.</small></span><button type="button" onClick={locate}><LocateFixed/> Vị trí hiện tại</button></div>
    <MapContainer center={HO_CHI_MINH_CENTER} zoom={12} scrollWheelZoom className="partner-map">
      <TileLayer attribution='&copy; OpenStreetMap contributors' url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"/>
      <ClickHandler onChange={onChange}/><Recenter location={value}/>
      {value&&<CircleMarker center={[value.latitude,value.longitude]} radius={10} pathOptions={{color:"#fff",fillColor:"#0d6b57",fillOpacity:1,weight:4}}/>}
    </MapContainer>
    {value?<p className="location-confirmed"><MapPin/> Đã chọn: {value.latitude.toFixed(6)}, {value.longitude.toFixed(6)}</p>:<p className="location-required">Chưa chọn vị trí trên bản đồ.</p>}
    {geoError&&<p className="location-required">{geoError}</p>}
  </div>;
}
