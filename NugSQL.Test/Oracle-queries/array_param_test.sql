-- :name get_users_by_id :many
select  *
    from oms."nugtest" u
    where u."id" in :ids