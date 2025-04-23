CREATE OR REPLACE VIEW public.vw_labels
 AS
 SELECT l.id,
    l.name,
    l.description,
    l.languageid,
    la.name AS language,
    l.isactive
   FROM label l
     JOIN language la ON l.languageid = la.id;

ALTER TABLE public.vw_labels
    OWNER TO lmdev_admin;