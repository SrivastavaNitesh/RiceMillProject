(()=>{
function init(){
 const form=document.getElementById('gatemanForm')||document.getElementById('record-edit-form');
 if(!form)return;
 const party=document.getElementById('txtPartyName')||document.getElementById('field-PartyId'),vehicle=document.getElementById('txtVehicleNo')||document.getElementById('field-VehicleNo'),driver=document.getElementById('txtDriverName')||document.getElementById('field-DriverName');
 if(!party||!vehicle||!driver)return;
 const mobile=document.getElementById('txtDriverMobile')||form.querySelector('[name="Values[DriverMobile]"]');
 const initial={party:party.value,vehicle:vehicle.value,driver:driver.value,mobile:mobile?.value||''};
 let mappings=[],serial=0,loading=false,failed=false;
 const refresh=el=>{if(window.jQuery&&jQuery.fn.select2)jQuery(el).trigger('change.select2');};
 const notice=document.createElement('div');notice.className='col-12 text-danger';notice.setAttribute('role','alert');notice.hidden=true;form.append(notice);
 const options=(el,rows,key,label,selected='',preserve=false)=>{
  el.replaceChildren();const blank=document.createElement('option');blank.value='';blank.textContent=el===vehicle?'Select Vehicle':'Select Driver';el.append(blank);
  const seen=new Set();for(const row of rows){const value=row[key]||'';if(!value||seen.has(value))continue;seen.add(value);const option=document.createElement('option');option.value=value;option.textContent=label(row);if(el===driver)option.dataset.mobile=row.driverMobile||'';el.append(option);}
  if(preserve&&selected&&!seen.has(selected)){const old=document.createElement('option');old.value=selected;old.textContent=selected;el.append(old);}
  el.value=[...el.options].some(o=>o.value===selected)?selected:'';refresh(el);
 };
 const vehicles=(rows,selected='',preserve=false)=>options(vehicle,rows,'vehicleNumber',r=>r.vehicleNumber,selected,preserve);
 const drivers=(rows,selected='',preserve=false)=>options(driver,rows,'driverName',r=>r.driverName+(r.driverMobile?' / '+r.driverMobile:''),selected,preserve);
 const syncMobile=()=>{if(mobile)mobile.value=driver.selectedOptions[0]?.dataset.mobile||((driver.value===initial.driver)?initial.mobile:'');};
 const unique=(rows,key)=>[...new Set(rows.map(r=>r[key]).filter(Boolean))];
 const chooseVehicle=()=>{
  const current=driver.value,rows=vehicle.value?mappings.filter(r=>r.vehicleNumber===vehicle.value):mappings;
  drivers(mappings,current);if(current&&!rows.some(r=>r.driverName===current))driver.value='';if(!driver.value&&unique(rows,'driverName').length===1)driver.value=unique(rows,'driverName')[0];refresh(driver);syncMobile();
 };
 const chooseDriver=()=>{
  const current=vehicle.value,rows=driver.value?mappings.filter(r=>r.driverName===driver.value):mappings;
  vehicles(mappings,current);if(current&&!rows.some(r=>r.vehicleNumber===current))vehicle.value='';if(!vehicle.value&&unique(rows,'vehicleNumber').length===1)vehicle.value=unique(rows,'vehicleNumber')[0];refresh(vehicle);syncMobile();
 };
 async function load(preserve=false){
  const request=++serial,selected={vehicle:vehicle.value,driver:driver.value},id=party.value;
  mappings=[];failed=false;notice.hidden=true;loading=!!id;
  vehicle.disabled=!!id;driver.disabled=!!id;
  if(!preserve){vehicles([]);drivers([]);syncMobile();}
  if(!id){loading=false;return;}
  try{
   const response=await fetch('/Gateman/GetEntryMappings?partyId='+encodeURIComponent(id));if(!response.ok)throw Error('Mapping request failed');const data=await response.json();if(request!==serial)return;
   if(!data.success||!Array.isArray(data.mappings))throw Error('Mapping request failed');mappings=data.mappings;
   vehicles(mappings,preserve?selected.vehicle:'',preserve);drivers(mappings,preserve?selected.driver:'',preserve);
   if(!preserve){if(unique(mappings,'vehicleNumber').length===1){vehicle.value=unique(mappings,'vehicleNumber')[0];refresh(vehicle);chooseVehicle();}else if(unique(mappings,'driverName').length===1){driver.value=unique(mappings,'driverName')[0];refresh(driver);chooseDriver();}}
   syncMobile();
  }catch(error){if(request!==serial)return;failed=true;notice.textContent='Vehicle/driver mapping could not be loaded. Please select the party again.';notice.hidden=false;}
  finally{if(request===serial){loading=false;vehicle.disabled=false;driver.disabled=false;}}
 }
 party.addEventListener('change',()=>load());vehicle.addEventListener('change',chooseVehicle);driver.addEventListener('change',chooseDriver);
 if(form.id==='record-edit-form'&&window.jQuery&&jQuery.fn.select2)jQuery([party,vehicle,driver]).on('select2:select select2:clear',function(){this.dispatchEvent(new Event('change'));});
 form.addEventListener('submit',event=>{if(loading||failed){event.preventDefault();notice.textContent=loading?'Please wait for the vehicle/driver mapping to load.':'Please select the party again to load vehicle/driver mappings.';notice.hidden=false;}});
 form.addEventListener('reset',()=>setTimeout(()=>{party.value=initial.party;vehicles([],initial.vehicle,true);drivers([],initial.driver,true);refresh(party);load(true);},0));
 if(party.value)load(true);
}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
})();
