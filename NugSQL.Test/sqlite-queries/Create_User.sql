-- :name create_user :scalar
insert into user(user_name, password, salt, profile, status)
values(:user_name, :password, :salt, :profile, :status);
SELECT last_insert_rowid();