import java.util.*;
public class Main {
 static class Repo {
  Map<Integer,Map<String,Object>> data=new HashMap<>();int next=1;
  int add(Map<String,Object> v){int id=next++;data.put(id,new HashMap<>(v));return id;}
  Map<String,Object> get(int id){return data.containsKey(id)?new HashMap<>(data.get(id)):null;}
  boolean update(int id,Map<String,Object> v){if(!data.containsKey(id))return false;data.put(id,new HashMap<>(v));return true;}
  boolean delete(int id){return data.remove(id)!=null;}
 }
 public static void main(String[] z){Repo r=new Repo();Map<String,Object> sedan=Map.of("type","Sedan","dailyRate",100);int id=r.add(sedan);if(!r.get(id).get("type").equals("Sedan"))throw new AssertionError();if(!r.update(id,Map.of("type","SUV","dailyRate",150))||!r.get(id).get("type").equals("SUV"))throw new AssertionError();if(!r.delete(id)||r.get(id)!=null)throw new AssertionError();System.out.println("Repository CRUD: PASS");}
}
