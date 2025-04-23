-- View: public.vw_uid

-- DROP VIEW public.vw_uid;

CREATE OR REPLACE VIEW public.vw_uid
 AS
 SELECT p.columndata ->> 'Uid'::text AS uid,
    p.columndata ->> 'Dept Code'::text AS dept_code,
    p.columndata ->> 'Dept Name'::text AS dept_name,
    p.columndata ->> 'Division Code'::text AS div_code,
    p.columndata ->> 'Division Name'::text AS div_name,
    p.columndata ->> 'Class Code'::text AS class_code,
    p.columndata ->> 'Class Name'::text AS class_name,
    p.columndata ->> 'Subclass Code'::text AS subclass_code,
    p.columndata ->> 'Subclass Name'::text AS subclass_name,
    p.columndata ->> 'Group Code'::text AS group_code,
    p.columndata ->> 'Group Name'::text AS group_name,
    p.columndata ->> 'Planogram/Family Code'::text AS planogram_family_code,
    btrim(concat(u.planogram, ' ', u.familyname), ' '::text) AS planogram_family_name,
    p.columndata ->> 'Season Code'::text AS season_code,
    s.name AS season_name,
    pg.name AS pricegrouping,
    pp.name AS productpurpose,
    p.columndata ->> 'Target MRP'::text AS target_mrp,
    p.columndata ->> 'Product Desc'::text AS product_desc,
    p.datecreated,
    p.pictureid,
    p.status,
    p.isactive
   FROM product p
     JOIN season s ON (p.columndata ->> 'Season Code'::text) = s.code
     LEFT JOIN uidinitiation u ON (p.columndata ->> 'Planogram/Family Code'::text) = u.planocode
     JOIN pricegrouping pg ON (p.columndata ->> 'Price Grouping'::text) = pg.code
     JOIN productpurpose pp ON (p.columndata ->> 'Product Purpose'::text) = pp.code
  WHERE p.columndata IS NOT NULL AND (p.columndata ->> 'Uid'::text) IS NOT NULL AND u.status = 'Approved'::text
  ORDER BY p.datecreated DESC;

ALTER TABLE public.vw_uid
    OWNER TO lmdev_admin;

