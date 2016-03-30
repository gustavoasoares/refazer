def product(n, term):
    if n == 1:
        return term(1)
    else:
        return term(n) * product(n-1)