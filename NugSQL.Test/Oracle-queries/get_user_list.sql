-- :name get_users :many
select  *
    from oms."nugtest" u
    where u."user_name" like :name
