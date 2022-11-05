-- :name create_tbl_user :affected

CREATE TABLE oms."nugtest"
(
    "id" int NOT NULL,
    "user_name" varchar2(150) NOT NULL,
    "salt" varchar2(24) NOT NULL,
    "password" varchar2(48) NOT NULL,
    "profile" varchar2(50) NOT NULL,
    "status" smallint NOT NULL,
    CONSTRAINT "user_pkey" PRIMARY KEY ("id"),
    CONSTRAINT "user_user_name_key" UNIQUE ("user_name")
)