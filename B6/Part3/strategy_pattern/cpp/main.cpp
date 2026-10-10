#include <algorithm>
#include <cmath>
#include <ctime>
#include <iomanip>
#include <iostream>
#include <set>
#include <stdexcept>
#include <string>
struct DT{std::tm t{};};
DT dt(int y,int m,int d,int h=0){DT x;x.t.tm_year=y-1900;x.t.tm_mon=m-1;x.t.tm_mday=d;x.t.tm_hour=h;std::mktime(&x.t);return x;}
std::time_t stamp(DT x){return std::mktime(&x.t);}
int days(DT p,DT r){double h=std::difftime(stamp(r),stamp(p))/3600.;return std::max(1,(int)std::ceil(std::ceil(h)/24.));}
struct Car{double daily,weekly,monthly;};
double base(Car c,int n){int rem=n;double a=0;if(c.monthly>0&&rem>=30){a+=(rem/30)*c.monthly;rem%=30;}if(c.weekly>0&&rem>=7){a+=(rem/7)*c.weekly;rem%=7;}return a+rem*c.daily;}
bool weekend(DT p,DT r){for(auto t=stamp(p);t<stamp(r);t+=86400){std::tm* d=std::localtime(&t);if(d&&(d->tm_wday==0||d->tm_wday==6))return true;}return false;}
struct Policy{virtual ~Policy()=default;virtual std::string name()const=0;virtual double price(Car,DT,DT)const=0;};
struct Standard:Policy{std::string name()const override{return "Standard";}double price(Car c,DT p,DT r)const override{return stamp(r)<=stamp(p)?0:base(c,days(p,r));}};
struct Weekend:Policy{std::string name()const override{return "Weekend";}double price(Car c,DT p,DT r)const override{double a=base(c,days(p,r));return weekend(p,r)?a*1.2:a;}};
struct Seasonal:Policy{std::set<int> months{12,1,4};std::string name()const override{return "Seasonal";}double price(Car c,DT p,DT r)const override{double a=base(c,days(p,r));return months.count(p.t.tm_mon+1)?a*1.3:a;}};
struct Selector{Standard s;Weekend w;Seasonal h;const Policy& select(DT p,DT r)const{if(h.months.count(p.t.tm_mon+1))return h;return weekend(p,r)?static_cast<const Policy&>(w):static_cast<const Policy&>(s);}};
int main(){Car c{100,600,2000};auto p=dt(2026,2,6,10),r=dt(2026,2,9,10);Selector s;auto&q=s.select(p,r);double a=q.price(c,p,r);if(q.name()!="Weekend"||a!=360)throw std::runtime_error("assertion failed");std::cout<<q.name()<<": "<<std::fixed<<std::setprecision(2)<<a<<"\n";}
