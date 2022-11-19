-- :name DeleteTestUsers :affected

delete from oms."nugtest"
where "id" < 0 or "user_name" in ('u1','u2')