-- :name get_users :many
select  *
    from user u
    where u.user_name like :name
