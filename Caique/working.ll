; ModuleID = 'main'
source_filename = "main"

declare i32 @printf(i8*, ...)

define double @main(i32) {
entry:
  %x = alloca i32
  %y = alloca double
  store i32 %0, i32* %x
  store double 5.300000e+00, double* %y
  store double 4.300000e+00, double* %y
  %yload = load double, double* %y
  %xload = load i32, i32* %x
  %tmpcast = sitofp i32 %xload to double
  %divtmp = fdiv double %yload, %tmpcast
  ret double %divtmp
}
