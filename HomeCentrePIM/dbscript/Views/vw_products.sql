CREATE OR REPLACE VIEW public.vw_products
 AS
 SELECT pd.id,
    pd.uiddata,
    pd.columndata,
    pd.errordata,
    pd.uid,
    pd.datecreated,
    pd.createdby,
    pd.isactive,
    pt.producttemplatestatusid,
    p.name AS producttemplatestatus
   FROM product pd
     LEFT JOIN producttemplate pt ON pd.id = pt.templateid
     LEFT JOIN producttemplatestatus p ON pt.producttemplatestatusid = p.id;

ALTER TABLE public.vw_products
    OWNER TO lmdev_admin;