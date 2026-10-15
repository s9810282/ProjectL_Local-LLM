/* Display metadata comes from Excel specifications and rows, not Unity enums. */
(function(root){
 'use strict';
 const COLUMNS=['Character_Key','Character_Name','Character_NameKOR','Character_Level','Character_RoleType','Character_PositionType','Character_HealthValue','Character_DefenseValue','Character_AttackValue','Character_MoveMethod','Character_MoveRange','Character_AttackMethod','Character_AttackRange'];
 const METHODS=['Method_Key','Method_Type','Method_ChoiceRange','Method_Splash','Method_SplashValue'];
 const ROLES={1:'일반',2:'기병',3:'수비병',4:'포병',5:'메인'},TYPES={1:'십자직선형',2:'대각직선형',3:'8방향 직선형',4:'부채형',5:'방사형'},CHOICE={1:'단일 대상',2:'방향 지정',3:'전체 지정'},SPLASH={0:'없음',1:'십자',2:'대각',3:'방사'};
 const palette=['#56c5b8','#edab67','#a4a0e9','#ed888e','#79b5e0','#b2d26f','#d78fd0','#d5c68c'];
 const colorOf=key=>palette[Math.abs(Number(key)||0)%palette.length],validColor=c=>typeof c==='string'&&/^#[0-9a-f]{6}$/i.test(c);
 function entries(rows,required,sheet){
  if(!Array.isArray(rows)||!rows.length)throw new Error(`${sheet} 헤더가 없어.`);
  for(const h of required)if(rows[0].filter(v=>v===h).length!==1)throw new Error(`${sheet}: ${h} 헤더를 확인해 줘.`);
  return rows.slice(1).filter(r=>r.some(v=>v!==null&&v!==undefined&&v!=='')).map(r=>Object.fromEntries(rows[0].map((h,i)=>[h,r[i]??null])));
 }
 function keyCheck(value,seen,sheet){if(!Number.isInteger(value)||value<1||value>2147483647)throw new Error(`${sheet}: 양의 정수 키가 필요해.`);if(seen.has(value))throw new Error(`${sheet}: 키 ${value}가 중복됐어.`);seen.add(value);}
 function specRoles(sheets){const result={...ROLES};for(const rows of sheets)for(const row of rows){if(!row.includes('Character_RoleType'))continue;for(const value of row)if(typeof value==='string')for(const m of value.matchAll(/(?:^|\n)\s*(\d+)\s*=\s*([^\n]+)/g))result[m[1]]=m[2].replace(/\s*\(.*\)\s*$/,'').trim();}return result;}
 function characters(rows,roles=ROLES){const seen=new Set();return entries(rows,['Character_Key'],'CharacterMaster').map(r=>{
  keyCheck(r.Character_Key,seen,'CharacterMaster');const data=Object.fromEntries(COLUMNS.map(h=>[h,r[h]??null]));
  for(const h of COLUMNS.slice(3))if(data[h]!==null&&(typeof data[h]!=='number'||!Number.isInteger(data[h])))throw new Error(`Character ${r.Character_Key}: ${h}는 정수여야 해.`);
  return {key:r.Character_Key,name:String(r.Character_NameKOR||r.Character_Name||`Character ${r.Character_Key}`),roleName:String(r.EditorCharacter_RoleName||roles[r.Character_RoleType]||(r.Character_RoleType===null||r.Character_RoleType===undefined?'미지정':`직업 ${r.Character_RoleType}`)),color:validColor(r.EditorCharacter_Color)?r.EditorCharacter_Color:colorOf(r.Character_Key),data};
 });}
 function methods(rows){const seen=new Set();return entries(rows,METHODS,'MethodMaster').map(r=>{keyCheck(r.Method_Key,seen,'MethodMaster');for(const h of METHODS)if(!Number.isInteger(r[h]))throw new Error(`Method ${r.Method_Key}: ${h}는 정수여야 해.`);return {key:r.Method_Key,type:r.Method_Type,choice:r.Method_ChoiceRange,splash:r.Method_Splash,splashValue:r.Method_SplashValue};});}
 const value=(c,field)=>c?.data?.[`Character_${field}`]??null,show=v=>v===null||v===undefined?'—':String(v);
 function methodInfo(key,methods){const m=methods.find(m=>m.key===key);if(!m)return key?`Method ${key} · 참조 데이터 없음`:'방식 미지정';return `${TYPES[m.type]||`종류 ${m.type}`} · ${CHOICE[m.choice]||`선택 ${m.choice}`} / 추가 ${SPLASH[m.splash]||m.splash}${m.splash?` ${m.splashValue}%`:''} (키 ${key})`;}
 function ranges(state,unit,c,methods,direction='North'){
  const calculate=(methodKey,range,move)=>{const m=methods.find(m=>m.key===methodKey);if(!m||!Number.isInteger(range)||range<1||!TYPES[m.type])return [];
   return state.cells.filter(cell=>{let dx=cell.x-unit.x,dy=cell.y-unit.y;const ax=Math.abs(dx),ay=Math.abs(dy),dist=Math.max(ax,ay);if(!dist||dist>range||(move&&cell.type==='Blocked'))return false;if(m.type===1)return !dx||!dy;if(m.type===2)return ax===ay;if(m.type===3)return !dx||!dy||ax===ay;if(m.type===5)return dx*dx+dy*dy<=range*range;if(direction==='East')[dx,dy]=[-dy,dx];else if(direction==='South')[dx,dy]=[-dx,-dy];else if(direction==='West')[dx,dy]=[dy,-dx];return dy>0&&Math.abs(dx)<=dy;}).map(cell=>({x:cell.x,y:cell.y}));
  };return {move:calculate(value(c,'MoveMethod'),value(c,'MoveRange'),true),attack:calculate(value(c,'AttackMethod'),value(c,'AttackRange'),false)};
 }
 function snapshot(catalog,methods){return {EditorCharacterMaster:[[...COLUMNS,'EditorCharacter_Color','EditorCharacter_RoleName'],...catalog.map(c=>[...COLUMNS.map(h=>h==='Character_Key'?c.key:c.data[h]??null),c.color,c.roleName])],EditorMethodMaster:[[...METHODS],...methods.map(m=>[m.key,m.type,m.choice,m.splash,m.splashValue])]};}
 const api={characters,methods,specRoles,colorOf,validColor,value,show,methodInfo,ranges,snapshot,TYPES};root.CombatReferences=api;if(typeof module!=='undefined'&&module.exports)module.exports=api;
})(typeof globalThis!=='undefined'?globalThis:window);
