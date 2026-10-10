const daysBetween = (p,r) => Math.max(1, Math.ceil((r-p)/86400000));
function basePrice(c,n) {
  let rem=n, amount=0;
  if(c.monthlyRate>0 && rem>=30){amount+=Math.floor(rem/30)*c.monthlyRate;rem%=30;}
  if(c.weeklyRate>0 && rem>=7){amount+=Math.floor(rem/7)*c.weeklyRate;rem%=7;}
  return amount+rem*c.dailyRate;
}
class Standard { get name(){return "Standard";} calculate(c,p,r){return r<=p?0:basePrice(c,daysBetween(p,r));} }
class Weekend {
  constructor(percent=20){this.percent=percent;} get name(){return "Weekend";}
  calculate(c,p,r){const a=basePrice(c,daysBetween(p,r));for(let d=new Date(p);d<r;d.setDate(d.getDate()+1)){if(d.getDay()===0||d.getDay()===6)return a*(1+this.percent/100);}return a;}
}
class Seasonal {
  constructor(months=[12,1,4],percent=30){this.months=new Set(months);this.percent=percent;} get name(){return "Seasonal";}
  calculate(c,p,r){const a=basePrice(c,daysBetween(p,r));return this.months.has(p.getMonth()+1)?a*(1+this.percent/100):a;}
}
class Selector {
  constructor(months=[12,1,4]){this.months=new Set(months);this.standard=new Standard();this.weekend=new Weekend();this.seasonal=new Seasonal(months);}
  select(p,r){if(this.months.has(p.getMonth()+1))return this.seasonal;for(let d=new Date(p);d<r;d.setDate(d.getDate()+1)){if(d.getDay()===0||d.getDay()===6)return this.weekend;}return this.standard;}
}
const car={dailyRate:100,weeklyRate:600,monthlyRate:2000}, p=new Date(2026,1,6,10), r=new Date(2026,1,9,10);
const policy=new Selector().select(p,r), amount=policy.calculate(car,p,r);
if(policy.name!=="Weekend"||amount!==360)throw Error("Strategy example failed");
console.log(`${policy.name}: ${amount.toFixed(2)}`);
