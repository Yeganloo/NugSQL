-- :name create_user :affected
insert into oms."nugtest"("id","user_name", "password", "salt", "profile", "status")
values(:id,:user_name, :password, :salt, :profile, :status)