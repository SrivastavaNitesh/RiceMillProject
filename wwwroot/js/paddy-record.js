(() => {
 'use strict';
 const form=document.getElementById('paddy-filters'),table=document.getElementById('paddy-table');
 if(!form||!table)return;
 const items=[...form.querySelectorAll('[data-paddy-item]')],locations=[...form.querySelectorAll('[data-paddy-location]')];
 const bagtypes=[...form.querySelectorAll('[data-paddy-bagtype]')];
 const extras=[...form.querySelectorAll('[data-paddy-optional]')];
 const loadedFrom=document.getElementById('FromDate').value,loadedTo=document.getElementById('ToDate').value;
 const selected=controls=>new Set(controls.filter(c=>c.checked).map(c=>c.value));
 function apply(persist=false){
  const itemIds=selected(items),locationIds=selected(locations),bagtypeIds=selected(bagtypes);
  table.querySelectorAll('[data-location][data-bagtype]').forEach(cell=>{cell.hidden=!(locationIds.has(cell.dataset.location)&&bagtypeIds.has(cell.dataset.bagtype));});
  table.querySelectorAll('[data-item]').forEach(cell=>{cell.hidden=!itemIds.has(cell.dataset.item);});
  table.querySelectorAll('[data-location-group]').forEach(cell=>{cell.hidden=!(locationIds.has(cell.dataset.locationGroup)&&bagtypeIds.size);cell.colSpan=Math.max(1,bagtypeIds.size);});
  const visibleExtras=new Map(extras.map(control=>[control.dataset.paddyOptional,control.checked]));
  table.querySelectorAll('[data-optional]').forEach(cell=>{cell.hidden=!visibleExtras.get(cell.dataset.optional);});
  table.querySelectorAll('[data-optional-group]').forEach(group=>{const count=group.dataset.groupMembers.split(',').filter(key=>visibleExtras.get(key)).length;group.hidden=count===0;group.colSpan=Math.max(1,count);});
  document.getElementById('paddy-item-summary').textContent=itemIds.size+' / '+items.length+' items selected';
  document.getElementById('paddy-location-summary').textContent=locationIds.size+' / '+locations.length+' locations selected';
  document.getElementById('paddy-bagtype-summary').textContent=bagtypeIds.size+' / '+bagtypes.length+' bag types selected';
  const empty=document.getElementById('paddy-empty');if(empty)empty.colSpan=3+bagtypeIds.size*locationIds.size+itemIds.size+extras.filter(control=>control.checked).length;
  if(persist){
   const url=new URL(window.location.href);url.searchParams.set('ColumnsChosen','true');url.searchParams.set('BagTypesChosen','true');url.searchParams.set('FromDate',loadedFrom);url.searchParams.set('ToDate',loadedTo);
   url.searchParams.delete('SelectedItemIds');url.searchParams.delete('SelectedLocationIds');url.searchParams.delete('SelectedBagTypeIds');
   itemIds.forEach(id=>url.searchParams.append('SelectedItemIds',id));locationIds.forEach(id=>url.searchParams.append('SelectedLocationIds',id));
   bagtypeIds.forEach(id=>url.searchParams.append('SelectedBagTypeIds',id));
   extras.forEach(control=>url.searchParams.set(control.dataset.paddyOptional,String(control.checked)));
   window.history.replaceState(null,'',url);
  }
 }
 [...items,...locations,...bagtypes,...extras].forEach(control=>control.addEventListener('change',()=>apply(true)));
 form.querySelectorAll('[data-pick]').forEach(button=>button.addEventListener('click',()=>{(button.dataset.pick==='item'?items:button.dataset.pick==='bagtype'?bagtypes:locations).forEach(c=>{c.checked=button.dataset.checked==='true';});apply(true);}));
 form.querySelectorAll('[data-all-optional]').forEach(button=>button.addEventListener('click',()=>{extras.forEach(control=>{control.checked=button.dataset.allOptional==='true';});apply(true);}));
 form.querySelectorAll('[data-picker-search]').forEach(input=>input.addEventListener('input',()=>{const term=input.value.trim().toLocaleLowerCase();form.querySelectorAll('[data-option="'+input.dataset.pickerSearch+'"]').forEach(option=>{option.hidden=!option.textContent.toLocaleLowerCase().includes(term);});}));
 const pickers=[...form.querySelectorAll('.paddy-picker')];
 document.addEventListener('click',event=>pickers.forEach(picker=>{if(!picker.contains(event.target))picker.open=false;}));
 pickers.forEach(picker=>picker.addEventListener('keydown',event=>{if(event.key==='Escape'){picker.open=false;picker.querySelector('summary').focus();}}));
 apply();

 // Build a separate, paper-sized register so app layout and browser scaling do not shrink it.
 function prepareRegisterPrint(){
  apply();
  const report=document.getElementById('paddy-record-page');
  let documentRoot=document.getElementById('paddy-print-document');
  if(!documentRoot){documentRoot=document.createElement('section');documentRoot.id='paddy-print-document';documentRoot.className='paddy-print-document';documentRoot.setAttribute('aria-hidden','true');document.body.appendChild(documentRoot);}
  documentRoot.replaceChildren();
  const source=table.cloneNode(true);source.querySelectorAll('[hidden]').forEach(node=>node.remove());
  const rows=[...source.tBodies[0].rows].filter(row=>!row.querySelector('#paddy-empty'));
  const totals=source.tFoot?.rows[0];
  const sample=rows[0]||totals;const columnCount=sample.cells.length;
  const pages=Math.max(1,Math.ceil(rows.length/23));
  function cleanCell(original){
   const cell=document.createElement(original.tagName.toLowerCase());cell.rowSpan=original.rowSpan;cell.colSpan=original.colSpan;
   const copy=original.cloneNode(true);copy.querySelectorAll('small').forEach(node=>node.remove());
   const locationNames=[...copy.querySelectorAll('[data-summary-location]')];
   cell.textContent=locationNames.length?locationNames.map(node=>node.textContent.trim()).join(', '):copy.textContent.trim().replace(/\s+/g,' ');
   if(original.classList.contains('qty'))cell.className='print-qty';
   if(original.tagName==='TH'){
    if(original.dataset.bagtype){const label=cell.textContent.toUpperCase().replace(/[^A-Z]/g,'');if(label==='PP'||label==='PPBAG'||label==='PPBAGS')cell.textContent='PP';else if(label==='JUTE'||label==='JUTEBAG'||label==='JUTEBAGS')cell.textContent='Jute';}
    if(original.dataset.mandatory==='rst')cell.textContent='RST';
    if(original.dataset.optional==='ShowLocations')cell.textContent='Location';
    if(original.dataset.optional==='ShowMeth')cell.textContent='Meth';
    if(original.dataset.item)cell.classList.add('print-item-header');
   }
   return cell;
  }
  function bodyRow(sourceRow,serial){
   const row=document.createElement('tr');const number=document.createElement('td');number.className='print-serial';number.textContent=serial;row.appendChild(number);
   if(sourceRow){[...sourceRow.cells].forEach(cell=>row.appendChild(cleanCell(cell)));}
   else{for(let i=0;i<columnCount;i++){const cell=document.createElement('td');cell.textContent='\u00a0';row.appendChild(cell);}}
   return row;
  }
  for(let page=0;page<pages;page++){
   const sheet=document.createElement('article');sheet.className='paddy-print-sheet';
   const grid=document.createElement('table');grid.className='paddy-print-grid';
   const weights=[3.2,...[...sample.cells].map(cell=>cell.dataset.mandatory==='party'?23:cell.dataset.mandatory==='vehicle'?12:cell.dataset.mandatory==='rst'?7:cell.dataset.optional==='ShowMeth'?12:cell.dataset.optional==='ShowLocations'?13:cell.dataset.optional==='ShowWeight'?10:7)];
   const totalWeight=weights.reduce((sum,value)=>sum+value,0);const columns=document.createElement('colgroup');
   weights.forEach(weight=>{const col=document.createElement('col');col.style.width=(100*weight/totalWeight)+'%';columns.appendChild(col);});grid.appendChild(columns);
   const head=document.createElement('thead');const titleRow=document.createElement('tr');titleRow.className='print-title-row';const titleCell=document.createElement('th');titleCell.colSpan=columnCount+1;
   const title=document.createElement('div');title.className='paddy-print-title';
   const name=document.createElement('strong');name.textContent=report?.dataset.printTitle||'PADDY PURCHASE REPORT';
   const supervisor=document.createElement('strong');supervisor.textContent=(report?.dataset.printSupervisor||'').toLocaleUpperCase();
   const date=document.createElement('strong');date.textContent='DATE: '+(report?.dataset.printDate||loadedTo);date.className='print-date';
   title.append(name,supervisor,date);titleCell.appendChild(title);titleRow.appendChild(titleCell);head.appendChild(titleRow);
   [...source.tHead.rows].forEach((original,index)=>{const row=document.createElement('tr');if(index===0){const number=document.createElement('th');number.textContent='S.No.';number.rowSpan=source.tHead.rows.length;number.className='print-serial';row.appendChild(number);}[...original.cells].forEach(cell=>row.appendChild(cleanCell(cell)));head.appendChild(row);});
   grid.appendChild(head);const body=document.createElement('tbody');const subset=rows.slice(page*23,(page+1)*23);
   for(let line=0;line<24;line++){
    const totalLine=page===pages-1&&line===subset.length;
    const row=bodyRow(totalLine?totals:subset[line],String(page*24+line+1));
    if(totalLine)row.classList.add('print-total-row');body.appendChild(row);
   }
   grid.appendChild(body);sheet.appendChild(grid);const footer=document.createElement('div');footer.className='paddy-print-footer';footer.textContent='Page '+(page+1)+' / '+pages;sheet.appendChild(footer);documentRoot.appendChild(sheet);
  }
 }
 window.addEventListener('beforeprint',prepareRegisterPrint);
 window.addEventListener('afterprint',()=>document.getElementById('paddy-print-document')?.replaceChildren());

})();
