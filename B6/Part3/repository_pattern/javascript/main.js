class InMemoryRepository {
  constructor(){this.items=new Map();this.nextId=1;}
  add(item){const id=this.nextId++;this.items.set(id,{...item});return id;}
  get(id){const v=this.items.get(id);return v?{...v}:null;}
  update(id,item){if(!this.items.has(id))return false;this.items.set(id,{...item});return true;}
  delete(id){return this.items.delete(id);}
}
const repo=new InMemoryRepository(), id=repo.add({type:"Sedan",dailyRate:100});
if(repo.get(id).type!=="Sedan")throw Error("get failed");
if(!repo.update(id,{type:"SUV",dailyRate:150})||repo.get(id).type!=="SUV")throw Error("update failed");
if(!repo.delete(id)||repo.get(id)!==null)throw Error("delete failed");
console.log("Repository CRUD: PASS");
