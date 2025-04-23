CREATE OR REPLACE VIEW public.vw_user_list_item
 AS
 SELECT u.id,
    u.name,
    u.email,
    u.mobile,
    u.isactive,
    u.genderid
   FROM appuser u;

ALTER TABLE public.vw_user_list_item
    OWNER TO lmdev_admin;