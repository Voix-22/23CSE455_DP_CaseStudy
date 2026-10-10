#include <iostream>
#include <map>
#include <optional>
#include <stdexcept>
#include <string>
struct Car{std::string type;int dailyRate;};
class Repo{std::map<int,Car> data;int next=1;public:int add(Car c){int id=next++;data[id]=c;return id;}std::optional<Car> get(int id)const{auto i=data.find(id);if(i==data.end())return {};return i->second;}bool update(int id,Car c){auto i=data.find(id);if(i==data.end())return false;i->second=c;return true;}bool remove(int id){return data.erase(id)>0;}};
int main(){Repo r;int id=r.add({"Sedan",100});if(!r.get(id)||r.get(id)->type!="Sedan")throw std::runtime_error("get failed");if(!r.update(id,{"SUV",150})||r.get(id)->type!="SUV")throw std::runtime_error("update failed");if(!r.remove(id)||r.get(id))throw std::runtime_error("delete failed");std::cout<<"Repository CRUD: PASS\n";}
