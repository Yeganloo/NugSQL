-- :name create_allType :scalar
insert into test."all_types"(my_int64, my_int32, my_int16, my_string, my_bytes, my_bool, my_json, my_single, my_double, my_bits, my_guid, "my_datetime")
values(:my_int64, :my_int32, :my_int16, :my_string, :my_bytes, :my_bool, :my_json, :my_single, :my_double, :my_bits, :my_guid, :my_datetime) RETURNING "my_int64"