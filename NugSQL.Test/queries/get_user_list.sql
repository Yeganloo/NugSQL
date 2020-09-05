-- :name get_users :many
select  *
    from test.user u
    where u.user_name like @name
