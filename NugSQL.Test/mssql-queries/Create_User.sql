-- :name create_user :scalar
insert into test."user"(user_name, password, salt, profile, status)
values(@user_name, @password, @salt, @profile, @status);
select 1;