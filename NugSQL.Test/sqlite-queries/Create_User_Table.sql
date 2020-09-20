-- :name create_tbl_user :affected

-- Table: test.user

CREATE TABLE "user"
(
    id integer primary key autoincrement,
    user_name Text NOT NULL,
    salt Blob NOT NULL,
    password Blob NOT NULL,
    profile Text NOT NULL,
    status integer NOT NULL
);
