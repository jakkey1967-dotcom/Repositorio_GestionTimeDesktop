<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">

  <!-- Identity transform -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Exclude .pdb files -->
  <xsl:key name="pdb-search" match="wix:Component[contains(wix:File/@Source, '.pdb')]" use="@Id" />
  <xsl:template match="wix:Component[key('pdb-search', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('pdb-search', @Id)]" />

  <!-- Exclude .xml documentation files -->
  <xsl:key name="xml-search" match="wix:Component[contains(wix:File/@Source, '.xml')]" use="@Id" />
  <xsl:template match="wix:Component[key('xml-search', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('xml-search', @Id)]" />

</xsl:stylesheet>
