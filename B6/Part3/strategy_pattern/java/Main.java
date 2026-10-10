import java.time.*;
import java.util.Set;
public class Main {
 record Car(double daily,double weekly,double monthly){}
 interface Policy { String name(); double price(Car c,LocalDateTime p,LocalDateTime r); }
 static int days(LocalDateTime p,LocalDateTime r){long sec=Duration.between(p,r).getSeconds();return (int)Math.max(1,Math.ceil(Math.ceil(sec/3600.0)/24.0));}
 static double base(Car c,int n){int rem=n;double a=0;if(c.monthly>0&&rem>=30){a+=(rem/30)*c.monthly;rem%=30;}if(c.weekly>0&&rem>=7){a+=(rem/7)*c.weekly;rem%=7;}return a+rem*c.daily;}
 static class Standard implements Policy {public String name(){return "Standard";}public double price(Car c,LocalDateTime p,LocalDateTime r){return r.isAfter(p)?base(c,days(p,r)):0;}}
 static class Weekend implements Policy {public String name(){return "Weekend";}public double price(Car c,LocalDateTime p,LocalDateTime r){double a=base(c,days(p,r));for(LocalDate d=p.toLocalDate();d.isBefore(r.toLocalDate());d=d.plusDays(1))if(d.getDayOfWeek()==DayOfWeek.SATURDAY||d.getDayOfWeek()==DayOfWeek.SUNDAY)return a*1.2;return a;}}
 static class Seasonal implements Policy {Set<Integer> months=Set.of(12,1,4);public String name(){return "Seasonal";}public double price(Car c,LocalDateTime p,LocalDateTime r){double a=base(c,days(p,r));return months.contains(p.getMonthValue())?a*1.3:a;}}
 static class Selector {Set<Integer> months=Set.of(12,1,4);Policy s=new Standard(),w=new Weekend(),h=new Seasonal();Policy select(LocalDateTime p,LocalDateTime r){if(months.contains(p.getMonthValue()))return h;for(LocalDate d=p.toLocalDate();d.isBefore(r.toLocalDate());d=d.plusDays(1))if(d.getDayOfWeek()==DayOfWeek.SATURDAY||d.getDayOfWeek()==DayOfWeek.SUNDAY)return w;return s;}}
 public static void main(String[] x){Car c=new Car(100,600,2000);var p=LocalDateTime.of(2026,2,6,10,0);var r=LocalDateTime.of(2026,2,9,10,0);Policy q=new Selector().select(p,r);double a=q.price(c,p,r);if(!q.name().equals("Weekend")||a!=360)throw new AssertionError();System.out.printf("%s: %.2f%n",q.name(),a);}
}
