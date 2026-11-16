"""Reference derivation of commands and effect hints from an act map.

Writes tests/fixtures/*.commands.tsv. An oracle for the .NET tool, not part of it.
Usage: python3 tests/reference/commands.py [repository root]
Needs: rdflib.
"""
import rdflib, sys, collections
from rdflib import RDF, Namespace
from rdflib.collection import Collection
D=Namespace('https://w3id.org/deed/ns#'); C=Namespace('https://w3id.org/deed/cli#'); XSD=rdflib.XSD
root=sys.argv[1] if len(sys.argv)>1 else '.'
def run(mapfile,out):
    g=rdflib.Graph()
    for f in ['vocabulary/deed.ttl','vocabulary/deed-cli.ttl',mapfile]: g.parse(f'{root}/{f}')
    tool=next(g.subjects(RDF.type,C.Tool)); cmd=str(g.value(tool,C.command))
    defres=g.value(tool,C.defaultResource); defverb=g.value(tool,C.defaultVerb)
    nm=lambda x: str(g.value(x,C.name) or g.value(x,D.name))
    def opt(x,name=None):
        s='--'+(name or nm(x)); sh=g.value(x,C.short); env=g.value(x,C.env)
        if sh: s+=f'|-{sh}'
        if env: s+=f'({env})'
        if g.value(x,D.repeatable) and str(g.value(x,D.repeatable))=='true': s+='*'
        return s
    rows=[]; paths=collections.Counter()
    for a in sorted(g.subjects(RDF.type,D.Act)):
        r=g.value(a,D.resource); chain=[]
        while r is not None: chain.insert(0,r); r=g.value(r,D.within)
        path=[nm(x) for x in chain if x!=defres]; v=g.value(a,D.verb)
        if v!=defverb: path.append(nm(v))
        ops=[];opts=[];flags=[];codes=[]
        def place(x,asop,name=None):
            if asop:
                o=nm(x)
                if str(g.value(x,D.repeatable))=='true': o+='...'
                if str(g.value(x,D.required))=='false': o='['+o+']'
                ops.append(o)
            elif g.value(x,D.datatype)==XSD.boolean: flags.append(opt(x,name))
            else: opts.append(opt(x,name))
        lead=[]
        for c in sorted(g.objects(a,D.context)):
            if c==D.Nothing: continue
            for s in sorted(g.objects(c,D.setting)):
                if g.value(s,C.bindAs)==C.Operand: lead.append(nm(s))
                else: place(s,False)
            for fl in g.objects(c,D.failure):
                ec=g.value(fl,C.exitCode)
                if ec is not None: codes.append(f'{g.value(fl,D.name)}={ec}')
        ops=lead+ops
        i=g.value(a,D.input)
        if i!=D.Nothing:
            for p in Collection(g,i):
                b=g.value(p,C.bindAs); role=g.value(p,D.role)
                place(p,(role==D.Subject and b!=C.Option) or b==C.Operand)
                fa=g.value(p,C.fileAlternative)
                if fa: opts.append('--'+str(fa))
        for s in g.objects(a,C.surfaceOption): place(s,False)
        for fl in g.objects(a,D.failure):
            ec=g.value(fl,C.exitCode) if fl!=D.Nothing else None
            if ec is not None: codes.append(f'{g.value(fl,D.name)}={ec}')
        ch=[str(g.value(c,D.manner)).split('#')[1] for c in g.objects(a,D.changes) if c!=D.Nothing]
        ro=not ch; de=any(m in('Converges','Removes') for m in ch); idem=ro or all(m in('Converges','Removes') for m in ch)
        rec=g.value(a,C.spelledAs)
        command='' if rec else ' '.join([cmd]+path)
        if command: paths[command]+=1
        # An act with cli:spelledAs has no command, so nothing of a command line: no operands,
        # options, flags or exit codes. Its hints stay: they belong to the act, not the CLI.
        if rec: ops=[];opts=[];flags=[];codes=[]
        rows.append((str(a),command,' '.join(ops),' '.join(sorted(set(opts))),' '.join(sorted(set(flags))),str(ro).lower(),str(de).lower(),str(idem).lower(),' '.join(sorted(set(codes))),'recorded spelling: '+str(rec) if rec else ''))
    dup=[p for p,n in paths.items() if n>1]; assert not dup,dup
    open(f'{root}/{out}','w').write('act\tcommand\toperands\toptions\tflags\tread-only\tdestructive\tidempotent\texit-codes\tnote\n'+''.join('\t'.join(r)+'\n' for r in rows))
    print(out,len(rows),'acts',sum(1 for r in rows if r[1]),'commands')
run('maps/varve/varve-acts.ttl','tests/fixtures/varve.commands.tsv')
run('maps/product-cli/product-acts.ttl','tests/fixtures/product-cli.commands.tsv')
