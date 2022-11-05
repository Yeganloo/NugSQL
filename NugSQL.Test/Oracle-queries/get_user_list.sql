-- :name get_users :many
select  *
    from oms."USERS" u
    where u.user_name like :name
