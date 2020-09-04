-- :name get_users :many
select  1+1 as "count", u.name
    from user u
    where u.name like @name
