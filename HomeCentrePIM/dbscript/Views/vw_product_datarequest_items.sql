CREATE OR REPLACE VIEW public.vw_product_datarequest_items
 AS
 SELECT p.id,
    p.uid,
    p.columndata,
    p.qcstatusid,
    dri.datarequestid,
    p.datecreated,
    p.isactive
   FROM product p
     JOIN datarequestitem dri ON dri.productid = p.id;

ALTER TABLE public.vw_product_datarequest_items
    OWNER TO lmdev_admin;
