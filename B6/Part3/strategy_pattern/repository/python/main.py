class InMemoryRepository:
    def __init__(self):
        self.items, self.next_id = {}, 1
    def add(self, item):
        key = self.next_id
        self.next_id += 1
        self.items[key] = dict(item)
        return key
    def get(self, key):
        value = self.items.get(key)
        return dict(value) if value is not None else None
    def update(self, key, item):
        if key not in self.items: return False
        self.items[key] = dict(item)
        return True
    def delete(self, key):
        return self.items.pop(key, None) is not None

if __name__ == "__main__":
    repo = InMemoryRepository()
    i = repo.add({"type":"Sedan", "daily_rate":100})
    assert repo.get(i)["type"] == "Sedan"
    assert repo.update(i, {"type":"SUV", "daily_rate":150})
    assert repo.get(i)["type"] == "SUV"
    assert repo.delete(i) and repo.get(i) is None
    print("Repository CRUD: PASS")
