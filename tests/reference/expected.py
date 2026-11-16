"""Reference SHACL results for the act maps and the broken fixtures, by pySHACL.

Writes tests/fixtures/*.expected.tsv. An oracle for the .NET tool, not part of it.
Usage: python3 tests/reference/expected.py [repository root]
Needs: rdflib, pyshacl (0.40.2 was used).
"""
import sys, collections, rdflib, pyshacl
from rdflib.namespace import SH, RDF
root=sys.argv[1] if len(sys.argv)>1 else '.'
def run(datafile,out):
    data=rdflib.Graph()
    for f in ['vocabulary/deed.ttl','vocabulary/deed-cli.ttl',datafile]: data.parse(f'{root}/{f}')
    shapes=rdflib.Graph(); shapes.parse(f'{root}/vocabulary/deed-shapes.ttl')
    _,rep,_=pyshacl.validate(data,shacl_graph=shapes,advanced=True,inference='none',allow_infos=True,allow_warnings=True)
    rows=[]
    for r in rep.subjects(RDF.type,SH.ValidationResult):
        f=rep.value(r,SH.focusNode); f=str(f) if isinstance(f,rdflib.URIRef) else '_:blank'
        s=rep.value(r,SH.sourceShape); s=str(s) if isinstance(s,rdflib.URIRef) else '_:property-shape'
        rows.append((rep.value(r,SH.resultSeverity).split('#')[1],f,s,' '.join(str(rep.value(r,SH.resultMessage)).split())))
    rows.sort()
    open(f'{root}/{out}','w').write('severity\tfocus\tshape\tmessage\n'+''.join('\t'.join(x)+'\n' for x in rows))
    print(out,dict(collections.Counter(x[0] for x in rows)))
run('maps/varve/varve-acts.ttl','tests/fixtures/varve.expected.tsv')
run('maps/product-cli/product-acts.ttl','tests/fixtures/product-cli.expected.tsv')
run('tests/fixtures/broken.ttl','tests/fixtures/broken.expected.tsv')
run('tests/fixtures/broken-terms.ttl','tests/fixtures/broken-terms.expected.tsv')
