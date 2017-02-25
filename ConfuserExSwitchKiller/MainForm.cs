using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using de4dot.blocks;
using de4dot.blocks.cflow;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace ConfuserExSwitchKiller
{
	// Token: 0x020002CD RID: 717
	public class MainForm : Form
	{
		// Token: 0x0600214C RID: 8524 RVA: 0x0008FA29 File Offset: 0x0008EA29
		public MainForm()
		{
			this.InitializeComponent();
		}

		// Token: 0x06002151 RID: 8529 RVA: 0x0008FCB4 File Offset: 0x0008ECB4
		public void AddMethods(TypeDef type)
		{
			if (type.HasMethods)
			{
				foreach (MethodDef current in type.Methods)
				{
					if (current.HasBody)
					{
						this.methods.Add(current);
					}
				}
			}
			if (type.HasNestedTypes)
			{
				foreach (TypeDef current2 in type.NestedTypes)
				{
					this.AddMethods(current2);
				}
			}
		}

		// Token: 0x0600214D RID: 8525 RVA: 0x0008FA60 File Offset: 0x0008EA60
		private void Button1Click(object sender, EventArgs e)
		{
			this.label2.Text = "";
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "Browse for target assembly";
			openFileDialog.InitialDirectory = "c:\\";
			if (this.DirectoryName != "")
			{
				openFileDialog.InitialDirectory = this.DirectoryName;
			}
			openFileDialog.Filter = "All files (*.exe,*.dll)|*.exe;*.dll";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				string fileName = openFileDialog.FileName;
				this.textBox1.Text = fileName;
				int num = fileName.LastIndexOf("\\");
				if (num != -1)
				{
					this.DirectoryName = fileName.Remove(num, fileName.Length - num);
				}
				if (this.DirectoryName.Length == 2)
				{
					this.DirectoryName += "\\";
				}
			}
		}

		// Token: 0x06002154 RID: 8532 RVA: 0x00090DF4 File Offset: 0x0008FDF4
		private void Button2Click(object sender, EventArgs e)
		{
			if (File.Exists(this.textBox1.Text))
			{
				string text = Path.GetDirectoryName(this.textBox1.Text);
				if (!text.EndsWith("\\"))
				{
					text += "\\";
				}
				string filename = text + Path.GetFileNameWithoutExtension(this.textBox1.Text) + "_deobfuscated" + Path.GetExtension(this.textBox1.Text);
				AssemblyDef assemblyDef = AssemblyDef.Load(this.textBox1.Text);
				ModuleDef manifestModule = assemblyDef.ManifestModule;
				if (manifestModule.IsILOnly)
				{
					ModuleWriterOptions moduleWriterOptions = new ModuleWriterOptions(manifestModule);
					moduleWriterOptions.MetaDataOptions.Flags |= (MetaDataFlags.PreserveTypeRefRids | MetaDataFlags.PreserveTypeDefRids | MetaDataFlags.PreserveFieldRids | MetaDataFlags.PreserveMethodRids | MetaDataFlags.PreserveParamRids | MetaDataFlags.PreserveMemberRefRids | MetaDataFlags.PreserveStandAloneSigRids | MetaDataFlags.PreserveEventRids | MetaDataFlags.PreservePropertyRids | MetaDataFlags.PreserveTypeSpecRids | MetaDataFlags.PreserveMethodSpecRids | MetaDataFlags.PreserveUSOffsets | MetaDataFlags.PreserveBlobOffsets | MetaDataFlags.PreserveExtraSignatureData | MetaDataFlags.KeepOldMaxStack);
					this.methods = new List<MethodDef>();
					if (manifestModule.HasTypes)
					{
						foreach (TypeDef current in manifestModule.Types)
						{
							this.AddMethods(current);
						}
					}
					BlocksCflowDeobfuscator blocksCflowDeobfuscator = new BlocksCflowDeobfuscator();
					for (int i = 0; i < this.methods.Count; i++)
					{
						Blocks blocks = new Blocks(this.methods[i]);
						blocksCflowDeobfuscator.Initialize(blocks);
						blocksCflowDeobfuscator.Deobfuscate();
						blocks.RepartitionBlocks();
						IList<Instruction> list;
						IList<ExceptionHandler> exceptionHandlers;
						blocks.GetCode(out list, out exceptionHandlers);
						DotNetUtils.RestoreBody(this.methods[i], list, exceptionHandlers);
					}
					for (int i = 0; i < this.methods.Count; i++)
					{
						for (int j = 0; j < this.methods[i].Body.Instructions.Count; j++)
						{
							if (this.methods[i].Body.Instructions[j].IsLdcI4() && j + 1 < this.methods[i].Body.Instructions.Count && this.methods[i].Body.Instructions[j + 1].OpCode == OpCodes.Pop)
							{
								this.methods[i].Body.Instructions[j].OpCode = OpCodes.Nop;
								this.methods[i].Body.Instructions[j + 1].OpCode = OpCodes.Nop;
								for (int k = 0; k < this.methods[i].Body.Instructions.Count; k++)
								{
									if (this.methods[i].Body.Instructions[k].OpCode == OpCodes.Br || this.methods[i].Body.Instructions[k].OpCode == OpCodes.Br_S)
									{
										if (this.methods[i].Body.Instructions[k].Operand is Instruction)
										{
											Instruction instruction = this.methods[i].Body.Instructions[k].Operand as Instruction;
											if (instruction == this.methods[i].Body.Instructions[j + 1])
											{
												if (k - 1 >= 0 && this.methods[i].Body.Instructions[k - 1].IsLdcI4())
												{
													this.methods[i].Body.Instructions[k - 1].OpCode = OpCodes.Nop;
												}
											}
										}
									}
								}
							}
							if (this.methods[i].Body.Instructions[j].OpCode == OpCodes.Dup && j + 1 < this.methods[i].Body.Instructions.Count && this.methods[i].Body.Instructions[j + 1].OpCode == OpCodes.Pop)
							{
								this.methods[i].Body.Instructions[j].OpCode = OpCodes.Nop;
								this.methods[i].Body.Instructions[j + 1].OpCode = OpCodes.Nop;
								for (int k = 0; k < this.methods[i].Body.Instructions.Count; k++)
								{
									if (this.methods[i].Body.Instructions[k].OpCode == OpCodes.Br || this.methods[i].Body.Instructions[k].OpCode == OpCodes.Br_S)
									{
										if (this.methods[i].Body.Instructions[k].Operand is Instruction)
										{
											Instruction instruction = this.methods[i].Body.Instructions[k].Operand as Instruction;
											if (instruction == this.methods[i].Body.Instructions[j + 1])
											{
												if (k - 1 >= 0 && this.methods[i].Body.Instructions[k - 1].OpCode == OpCodes.Dup)
												{
													this.methods[i].Body.Instructions[k - 1].OpCode = OpCodes.Nop;
												}
											}
										}
									}
								}
							}
						}
					}
					for (int i = 0; i < this.methods.Count; i++)
					{
						Blocks blocks = new Blocks(this.methods[i]);
						blocksCflowDeobfuscator.Initialize(blocks);
						blocksCflowDeobfuscator.Deobfuscate();
						blocks.RepartitionBlocks();
						IList<Instruction> list;
						IList<ExceptionHandler> exceptionHandlers;
						blocks.GetCode(out list, out exceptionHandlers);
						DotNetUtils.RestoreBody(this.methods[i], list, exceptionHandlers);
					}
					for (int i = 0; i < this.methods.Count; i++)
					{
						List<Instruction> list2 = new List<Instruction>();
						List<Instruction> list3 = new List<Instruction>();
						Local local = null;
						List<int> list4 = new List<int>();
						List<int> list5 = new List<int>();
						for (int j = 0; j < this.methods[i].Body.Instructions.Count; j++)
						{
							if (j + 3 < this.methods[i].Body.Instructions.Count && this.methods[i].Body.Instructions[j].IsLdcI4())
							{
                                if (this.methods[i].Body.Instructions[j + 1].IsLdcI4())
                                //if (this.methods[i].Body.Instructions[j + 1].OpCode == OpCodes.Xor)
								{
									//if (this.methods[i].Body.Instructions[j + 2].OpCode == OpCodes.Dup)
                                    if (this.methods[i].Body.Instructions[j + 2].OpCode == OpCodes.Xor)
									{
										//if (this.methods[i].Body.Instructions[j + 3].IsStloc())
										{
											//if (this.methods[i].Body.Instructions[j + 4].IsLdcI4())
											{
												//if (this.methods[i].Body.Instructions[j + 5].OpCode == OpCodes.Rem_Un)
												{
                                                    //if (this.methods[i].Body.Instructions[j + 6].OpCode == OpCodes.Switch)
                                                    if (this.methods[i].Body.Instructions[j + 3].OpCode == OpCodes.Switch)
													{
														list2.Add(this.methods[i].Body.Instructions[j]);
														list4.Add(this.methods[i].Body.Instructions[j].GetLdcI4Value());
														//local = this.methods[i].Body.Instructions[j + 3].GetLocal(this.methods[i].Body.Variables);
														list5.Add(this.methods[i].Body.Instructions[j + 1].GetLdcI4Value());
														list3.Add(this.methods[i].Body.Instructions[j + 3]);
													}
												}
											}
										}
									}
								}
                                 /*
                                if (this.methods[i].Body.Instructions[j + 1].OpCode == OpCodes.Xor)
                                {
                                    if (this.methods[i].Body.Instructions[j + 2].OpCode == OpCodes.Dup)
                                    {
                                        if (this.methods[i].Body.Instructions[j + 3].IsStloc())
                                        {
                                            if (this.methods[i].Body.Instructions[j + 4].IsLdcI4())
                                            {
                                                if (this.methods[i].Body.Instructions[j + 5].OpCode == OpCodes.Rem_Un)
                                                {
                                                    if (this.methods[i].Body.Instructions[j + 6].OpCode == OpCodes.Switch)
                                                    {
                                                        list2.Add(this.methods[i].Body.Instructions[j]);
                                                        list4.Add(this.methods[i].Body.Instructions[j].GetLdcI4Value());
                                                        local = this.methods[i].Body.Instructions[j + 3].GetLocal(this.methods[i].Body.Variables);
                                                        list5.Add(this.methods[i].Body.Instructions[j + 4].GetLdcI4Value());
                                                        list3.Add(this.methods[i].Body.Instructions[j + 6]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                */

							}
						}
						if (list2.Count > 0)
						{
							for (int j = 0; j < this.methods[i].Body.Instructions.Count; j++)
							{
								if (j + 1 < this.methods[i].Body.Instructions.Count && this.methods[i].Body.Instructions[j].IsLdcI4())
								{
									if (this.methods[i].Body.Instructions[j + 1].IsBr())
									{
										Instruction instruction = this.methods[i].Body.Instructions[j + 1].Operand as Instruction;
										for (int k = 0; k < list2.Count; k++)
										{
											if (instruction == list2[k])
											{
												MethodDef methodDef = this.methods[i];
												int ldcI4Value = this.methods[i].Body.Instructions[j].GetLdcI4Value();
												uint num = (uint)(ldcI4Value ^ list4[k]);
												uint num2 = num % (uint)list5[k];
												this.methods[i].Body.Instructions[j].OpCode = OpCodes.Ldc_I4;
												this.methods[i].Body.Instructions[j].Operand = (int)num;
												this.methods[i].Body.Instructions[j + 1].Operand = OpCodes.Br;
												Instruction[] array = list3[k].Operand as Instruction[];
												this.methods[i].Body.Instructions[j + 1].Operand = array[(int)((UIntPtr)num2)];
												//this.methods[i].Body.Instructions.Insert(j + 1, OpCodes.Stloc_S.ToInstruction(local));
												j++;
											}
										}
									}
								}
							}
							this.methods[i].Body.SimplifyBranches();
							this.methods[i].Body.OptimizeBranches();
						}
					}
					for (int i = 0; i < this.methods.Count; i++)
					{
						Blocks blocks = new Blocks(this.methods[i]);
						blocksCflowDeobfuscator.Initialize(blocks);
						blocksCflowDeobfuscator.Deobfuscate();
						blocks.RepartitionBlocks();
						IList<Instruction> list;
						IList<ExceptionHandler> exceptionHandlers;
						blocks.GetCode(out list, out exceptionHandlers);
						DotNetUtils.RestoreBody(this.methods[i], list, exceptionHandlers);
					}
					for (int i = 0; i < this.methods.Count; i++)
					{
						Dictionary<Instruction, Instruction> dictionary = new Dictionary<Instruction, Instruction>();
						for (int j = 0; j < this.methods[i].Body.Instructions.Count; j++)
						{
							if (this.methods[i].Body.Instructions[j].IsConditionalBranch())
							{
								Instruction instruction2 = this.methods[i].Body.Instructions[j];
								for (int k = 0; k < this.methods[i].Body.Instructions.Count; k++)
								{
									if (this.methods[i].Body.Instructions[k].IsBr())
									{
										Instruction instruction3 = this.methods[i].Body.Instructions[k];
										Instruction instruction4 = this.methods[i].Body.Instructions[k].Operand as Instruction;
										if (instruction4 == instruction2)
										{
											if (!dictionary.ContainsKey(instruction4))
											{
												this.methods[i].Body.Instructions[k].OpCode = instruction2.GetOpCode();
												this.methods[i].Body.Instructions[k].Operand = instruction2.GetOperand();
												this.methods[i].Body.Instructions.Insert(k + 1, OpCodes.Br.ToInstruction(this.methods[i].Body.Instructions[j + 1]));
												k++;
												dictionary.Add(instruction4, this.methods[i].Body.Instructions[k]);
											}
										}
									}
								}
							}
						}
						this.methods[i].Body.SimplifyBranches();
						this.methods[i].Body.OptimizeBranches();
					}
					for (int i = 0; i < this.methods.Count; i++)
					{
						Blocks blocks = new Blocks(this.methods[i]);
						blocksCflowDeobfuscator.Initialize(blocks);
						blocksCflowDeobfuscator.Deobfuscate();
						blocks.RepartitionBlocks();
						IList<Instruction> list;
						IList<ExceptionHandler> exceptionHandlers;
						blocks.GetCode(out list, out exceptionHandlers);
						DotNetUtils.RestoreBody(this.methods[i], list, exceptionHandlers);
					}
					int num3 = 0;
					for (int i = 0; i < this.methods.Count; i++)
					{
						this.toberemoved = new List<int>();
						this.integer_values_1 = new List<int>();
						this.for_rem = new List<int>();
						this.switchinstructions = new List<Instruction>();
						for (int j = 0; j < this.methods[i].Body.Instructions.Count; j++)
						{
							if (j + 6 < this.methods[i].Body.Instructions.Count && this.methods[i].Body.Instructions[j].IsLdcI4())
							{
								if (this.methods[i].Body.Instructions[j + 1].OpCode == OpCodes.Xor)
								{
									//if (this.methods[i].Body.Instructions[j + 2].OpCode == OpCodes.Dup)
									{
										//if (this.methods[i].Body.Instructions[j + 3].IsStloc())
										{
											//if (this.methods[i].Body.Instructions[j + 4].IsLdcI4())
											{
												//if (this.methods[i].Body.Instructions[j + 5].OpCode == OpCodes.Rem_Un)
												{
													if (this.methods[i].Body.Instructions[j + 6].OpCode == OpCodes.Switch)
													{
														this.toberemoved.Add(j);
														this.integer_values_1.Add(this.methods[i].Body.Instructions[j].GetLdcI4Value());
														this.local_variable = this.methods[i].Body.Instructions[j + 3].GetLocal(this.methods[i].Body.Variables);
														this.for_rem.Add(this.methods[i].Body.Instructions[j + 4].GetLdcI4Value());
														this.switchinstructions.Add(this.methods[i].Body.Instructions[j + 6]);

                                                        //list2.Add(this.methods[i].Body.Instructions[j]);
                                                        //list4.Add(this.methods[i].Body.Instructions[j].GetLdcI4Value());
                                                        ////local = this.methods[i].Body.Instructions[j + 3].GetLocal(this.methods[i].Body.Variables);
                                                        //list5.Add(this.methods[i].Body.Instructions[j + 1].GetLdcI4Value());
                                                        //list3.Add(this.methods[i].Body.Instructions[j + 3]);
                                                    }
                                                }
											}
										}
									}
								}
							}
						}
						if (this.switchinstructions.Count > 0)
						{
							this.toberemovedindex = new List<int>();
							this.toberemovedvalues = new List<int>();
							this.conditionalinstructions = new List<Instruction>();
							this.brinstructions = new List<Instruction>();
							this.realbrinstructions = new List<Instruction>();
							this.local_values = new List<int>();
							this.instructions = this.methods[i].Body.Instructions;
							this.method = this.methods[i];
							this.InstructionParse2(0, 0u);
							num3 += this.toberemovedindex.Count;
							if (this.toberemovedindex.Count > 0)
							{
								for (int l = 0; l < this.toberemoved.Count; l++)
								{
									for (int j = 0; j < 6; j++)
									{
										this.methods[i].Body.Instructions[j + this.toberemoved[l]].OpCode = OpCodes.Nop;
										this.methods[i].Body.Instructions[j + this.toberemoved[l]].Operand = null;
									}
								}
								for (int j = 0; j < this.toberemovedindex.Count; j++)
								{
									this.methods[i].Body.Instructions[this.toberemovedindex[j]].OpCode = OpCodes.Ldc_I4;
									this.methods[i].Body.Instructions[this.toberemovedindex[j]].Operand = this.toberemovedvalues[j];
									if (!this.methods[i].Body.Instructions[this.toberemovedindex[j] + 1].IsBr())
									{
										for (int k = 0; k < 4; k++)
										{
											this.methods[i].Body.Instructions[this.toberemovedindex[j] + k + 1].OpCode = OpCodes.Nop;
											this.methods[i].Body.Instructions[this.toberemovedindex[j] + k + 1].Operand = null;
										}
									}
								}
							}
						}
						this.toberemoved = new List<int>();
						this.integer_values_1 = new List<int>();
						this.for_rem = new List<int>();
						this.switchinstructions = new List<Instruction>();
						for (int j = 0; j < this.methods[i].Body.Instructions.Count; j++)
						{
							if (j + 6 < this.methods[i].Body.Instructions.Count && this.methods[i].Body.Instructions[j].IsLdcI4())
							{
								if (this.methods[i].Body.Instructions[j + 1].OpCode == OpCodes.Xor)
								{
									if (this.methods[i].Body.Instructions[j + 2].IsLdcI4())
									{
										//if (this.methods[i].Body.Instructions[j + 3].OpCode == OpCodes.Rem_Un)
										{
											if (this.methods[i].Body.Instructions[j + 4].OpCode == OpCodes.Switch)
											{
												this.toberemoved.Add(j);
												this.integer_values_1.Add(this.methods[i].Body.Instructions[j].GetLdcI4Value());
												this.for_rem.Add(this.methods[i].Body.Instructions[j + 2].GetLdcI4Value());
												this.switchinstructions.Add(this.methods[i].Body.Instructions[j + 4]);
											}
										}
									}
								}
							}
						}
						if (this.switchinstructions.Count > 0)
						{
							this.toberemovedindex = new List<int>();
							this.toberemovedvalues = new List<int>();
							this.conditionalinstructions = new List<Instruction>();
							this.brinstructions = new List<Instruction>();
							this.realbrinstructions = new List<Instruction>();
							this.instructions = this.methods[i].Body.Instructions;
							this.method = this.methods[i];
							this.InstructionParseNoLocal(0);
							num3 += this.toberemovedindex.Count;
							if (this.toberemovedindex.Count > 0)
							{
								for (int l = 0; l < this.toberemoved.Count; l++)
								{
									for (int j = 0; j < 4; j++)
									{
										this.methods[i].Body.Instructions[j + this.toberemoved[l]].OpCode = OpCodes.Nop;
										this.methods[i].Body.Instructions[j + this.toberemoved[l]].Operand = null;
									}
								}
								for (int j = 0; j < this.toberemovedindex.Count; j++)
								{
									this.methods[i].Body.Instructions[this.toberemovedindex[j]].OpCode = OpCodes.Ldc_I4;
									this.methods[i].Body.Instructions[this.toberemovedindex[j]].Operand = this.toberemovedvalues[j];
									if (!this.methods[i].Body.Instructions[this.toberemovedindex[j] + 1].IsBr())
									{
										for (int k = 0; k < 4; k++)
										{
											this.methods[i].Body.Instructions[this.toberemovedindex[j] + k + 1].OpCode = OpCodes.Nop;
											this.methods[i].Body.Instructions[this.toberemovedindex[j] + k + 1].Operand = null;
										}
									}
								}
							}
						}
						Blocks blocks = new Blocks(this.methods[i]);
						blocksCflowDeobfuscator.Initialize(blocks);
						blocksCflowDeobfuscator.Deobfuscate();
						blocks.RepartitionBlocks();
						IList<Instruction> list;
						IList<ExceptionHandler> exceptionHandlers;
						blocks.GetCode(out list, out exceptionHandlers);
						DotNetUtils.RestoreBody(this.methods[i], list, exceptionHandlers);
						this.methods[i].Body.SimplifyBranches();
						this.methods[i].Body.OptimizeBranches();
					}
					for (int i = 0; i < this.methods.Count; i++)
					{
						Blocks blocks = new Blocks(this.methods[i]);
						blocksCflowDeobfuscator.Initialize(blocks);
						blocksCflowDeobfuscator.Deobfuscate();
						blocks.RepartitionBlocks();
						IList<Instruction> list;
						IList<ExceptionHandler> exceptionHandlers;
						blocks.GetCode(out list, out exceptionHandlers);
						DotNetUtils.RestoreBody(this.methods[i], list, exceptionHandlers);
					}
					moduleWriterOptions.Logger = DummyLogger.NoThrowInstance;
					manifestModule.Write(filename, moduleWriterOptions);
					this.label2.Text = "File deobfuscated! " + num3.ToString() + " replaces made!";
				}
			}
		}

		// Token: 0x06002150 RID: 8528 RVA: 0x0008FCAB File Offset: 0x0008ECAB
		private void Button3Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		// Token: 0x06002155 RID: 8533 RVA: 0x00092A98 File Offset: 0x00091A98
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this.components != null)
				{
					this.components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		// Token: 0x06002156 RID: 8534 RVA: 0x00092AD4 File Offset: 0x00091AD4
		private void InitializeComponent()
		{
			this.button3 = new Button();
			this.button2 = new Button();
			this.button1 = new Button();
			this.label2 = new Label();
			this.label1 = new Label();
			this.textBox1 = new TextBox();
			base.SuspendLayout();
			this.button3.Location = new Point(393, 96);
			this.button3.Name = "button3";
			this.button3.Size = new Size(72, 26);
			this.button3.TabIndex = 29;
			this.button3.Text = "Exit";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new EventHandler(this.Button3Click);
			this.button2.Location = new Point(224, 96);
			this.button2.Name = "button2";
			this.button2.Size = new Size(90, 26);
			this.button2.TabIndex = 28;
			this.button2.Text = "Deobfuscate";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new EventHandler(this.Button2Click);
			this.button1.Location = new Point(46, 96);
			this.button1.Name = "button1";
			this.button1.Size = new Size(123, 26);
			this.button1.TabIndex = 27;
			this.button1.Text = "Browse for assembly";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new EventHandler(this.Button1Click);
			this.label2.ForeColor = Color.Blue;
			this.label2.Location = new Point(112, 71);
			this.label2.Name = "label2";
			this.label2.Size = new Size(293, 22);
			this.label2.TabIndex = 26;
			this.label2.Text = "Current status";
			this.label1.BackColor = Color.Transparent;
			this.label1.ForeColor = Color.Black;
			this.label1.Location = new Point(46, 31);
			this.label1.Name = "label1";
			this.label1.Size = new Size(100, 14);
			this.label1.TabIndex = 25;
			this.label1.Text = "Name of assembly:";
			this.textBox1.AllowDrop = true;
			this.textBox1.Location = new Point(46, 48);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new Size(419, 20);
			this.textBox1.TabIndex = 24;
			this.textBox1.DragDrop += new DragEventHandler(this.TextBox1DragDrop);
			this.textBox1.DragEnter += new DragEventHandler(this.TextBox1DragEnter);
			base.AutoScaleDimensions = new SizeF(6f, 13f);
			base.AutoScaleMode = AutoScaleMode.Font;
			base.ClientSize = new Size(511, 152);
			base.Controls.Add(this.button3);
			base.Controls.Add(this.button2);
			base.Controls.Add(this.button1);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.label1);
			base.Controls.Add(this.textBox1);
			base.Name = "MainForm";
			this.Text = "ConfuserEx 5.0 Switch Killer 1.0 by CodeCracker";
			base.ResumeLayout(false);
			base.PerformLayout();
		}

		// Token: 0x06002152 RID: 8530 RVA: 0x0008FD90 File Offset: 0x0008ED90
		public void InstructionParse2(int ins_index, uint local_value)
		{
			for (int i = ins_index; i < this.instructions.Count; i++)
			{
				Instruction instruction = this.instructions[i];
				MethodDef methodDef = this.method;
				string text = methodDef.Name;
				string fullName = methodDef.DeclaringType.FullName;
				if (!this.toberemovedindex.Contains(i))
				{
					if (this.instructions[i].IsBr())
					{
						Instruction item = this.instructions[i].Operand as Instruction;
						if (!this.brinstructions.Contains(item) && !this.realbrinstructions.Contains(item))
						{
							this.realbrinstructions.Add(item);
							int ins_index2 = this.instructions.IndexOf(item);
							this.InstructionParse2(ins_index2, local_value);
						}
						break;
					}
					if (this.instructions[i].IsConditionalBranch() || this.instructions[i].IsLeave())
					{
						Instruction item = this.instructions[i].Operand as Instruction;
						if (!this.conditionalinstructions.Contains(item))
						{
							this.conditionalinstructions.Add(item);
							int ins_index3 = this.instructions.IndexOf(item);
							this.InstructionParse2(ins_index3, local_value);
							if (i + 1 < this.instructions.Count)
							{
								int ins_index4 = i + 1;
								this.InstructionParse2(ins_index4, local_value);
							}
						}
					}
					else
					{
						if (this.instructions[i].OpCode == OpCodes.Ret)
						{
							break;
						}
						if (this.instructions[i].IsLdcI4() && i + 1 < this.instructions.Count && this.instructions[i + 1].IsStloc() && this.instructions[i + 1].GetLocal(this.method.Body.Variables) == this.local_variable)
						{
							local_value = (uint)this.instructions[i].GetLdcI4Value();
						}
						else if (this.instructions[i].IsLdcI4() || (this.instructions[i].IsLdloc() && this.instructions[i].GetLocal(this.method.Body.Variables) == this.local_variable))
						{
							uint num;
							if (this.instructions[i].IsLdcI4())
							{
								num = (uint)this.instructions[i].GetLdcI4Value();
							}
							else
							{
								num = local_value;
							}
							int num2 = i + 1;
							if (this.instructions[i + 1].IsBr())
							{
								Instruction item2 = this.instructions[i + 1].Operand as Instruction;
								num2 = this.instructions.IndexOf(item2);
							}
							if (this.instructions[num2].IsLdcI4() || (this.instructions[num2].IsLdloc() && this.instructions[num2].GetLocal(this.method.Body.Variables) == this.local_variable))
							{
								uint num3;
								if (this.instructions[num2].IsLdcI4())
								{
									num3 = (uint)this.instructions[num2].GetLdcI4Value();
								}
								else
								{
									num3 = local_value;
								}
								uint num4 = 0u;
								if ((this.instructions[num2 + 1].OpCode == OpCodes.Mul && this.instructions[num2 + 2].IsLdcI4()) || (this.instructions[num2 + 1].IsLdcI4() && this.instructions[num2 + 2].OpCode == OpCodes.Mul) || this.instructions[num2 + 1].OpCode == OpCodes.Xor)
								{
									if (this.instructions[num2 + 1].OpCode != OpCodes.Xor)
									{
										if (this.instructions[num2 + 1].OpCode == OpCodes.Mul && this.instructions[num2 + 2].IsLdcI4())
										{
											num4 = (uint)this.instructions[num2 + 2].GetLdcI4Value();
										}
										if (this.instructions[num2 + 1].IsLdcI4() && this.instructions[num2 + 2].OpCode == OpCodes.Mul)
										{
											num4 = (uint)this.instructions[num2 + 1].GetLdcI4Value();
										}
									}
									if (this.instructions[num2 + 3].OpCode == OpCodes.Xor || this.instructions[num2 + 1].OpCode == OpCodes.Xor)
									{
										for (int j = 0; j < this.toberemoved.Count; j++)
										{
											if ((this.instructions[num2 + 4].IsBr() && this.instructions[num2 + 4].Operand as Instruction == this.instructions[this.toberemoved[j]]) || num2 + 4 == this.toberemoved[j] || (this.instructions[num2 + 1].OpCode == OpCodes.Xor && num2 == this.toberemoved[j]))
											{
												uint num5;
												if (this.instructions[num2 + 1].OpCode == OpCodes.Xor)
												{
													num5 = (num ^ num3);
												}
												else if (this.instructions[num2 + 1].IsLdcI4() || this.instructions[num2 + 1].IsLdloc())
												{
													num5 = (num4 * num3 ^ num);
												}
												else
												{
													num5 = (num * num3 ^ num4);
												}
												if (this.instructions[num2 + 1].OpCode != OpCodes.Xor)
												{
													local_value = (num5 ^ (uint)this.integer_values_1[j]);
												}
												else
												{
													local_value = num5;
												}
												uint num6 = local_value % (uint)this.for_rem[j];
												Instruction[] array = this.switchinstructions[j].Operand as Instruction[];
												Instruction item3 = array[(int)((UIntPtr)num6)];
												if (this.toberemovedindex.Contains(i))
												{
												}
												this.toberemovedindex.Add(i);
												this.toberemovedvalues.Add((int)num6);
												bool flag = false;
												int num7 = this.brinstructions.IndexOf(item3);
												if (num7 != -1)
												{
													int num8 = this.local_values[num7];
													if ((long)num8 != (long)((ulong)local_value))
													{
														flag = true;
													}
												}
												else
												{
													flag = true;
												}
												if (flag)
												{
													this.brinstructions.Add(item3);
													this.local_values.Add((int)local_value);
													this.InstructionParse2(this.instructions.IndexOf(item3), local_value);
													break;
												}
											}
										}
									}
								}
							}
						}
						else if (this.instructions[i].OpCode == OpCodes.Switch)
						{
							bool flag2;
							if (i - 4 < 0)
							{
								flag2 = false;
							}
							else
							{
								flag2 = false;
								for (int j = 0; j < this.toberemoved.Count; j++)
								{
									int num9 = this.toberemoved[j];
									if (i - 6 == this.toberemoved[j])
									{
										flag2 = true;
										break;
									}
								}
							}
							if (!flag2)
							{
								Instruction[] array2 = this.instructions[i].Operand as Instruction[];
								for (int j = 0; j < array2.Length; j++)
								{
									Instruction item4 = array2[j];
									this.InstructionParse2(this.instructions.IndexOf(item4), local_value);
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06002153 RID: 8531 RVA: 0x0009067C File Offset: 0x0008F67C
		public void InstructionParseNoLocal(int ins_index)
		{
			for (int i = ins_index; i < this.instructions.Count; i++)
			{
				Instruction instruction = this.instructions[i];
				MethodDef methodDef = this.method;
				if (!this.toberemovedindex.Contains(i))
				{
					if (this.instructions[i].IsBr())
					{
						Instruction item = this.instructions[i].Operand as Instruction;
						if (!this.brinstructions.Contains(item) && !this.realbrinstructions.Contains(item))
						{
							this.realbrinstructions.Add(item);
							int ins_index2 = this.instructions.IndexOf(item);
							this.InstructionParseNoLocal(ins_index2);
						}
						break;
					}
					if (this.instructions[i].IsConditionalBranch() || this.instructions[i].IsLeave())
					{
						Instruction item = this.instructions[i].Operand as Instruction;
						if (!this.conditionalinstructions.Contains(item))
						{
							this.conditionalinstructions.Add(item);
							int ins_index3 = this.instructions.IndexOf(item);
							this.InstructionParseNoLocal(ins_index3);
							if (i + 1 < this.instructions.Count)
							{
								int ins_index4 = i + 1;
								this.InstructionParseNoLocal(ins_index4);
							}
						}
					}
					else
					{
						if (this.instructions[i].OpCode == OpCodes.Ret)
						{
							break;
						}
						if (this.instructions[i].IsLdcI4())
						{
							uint num = 0u;
							if (this.instructions[i].IsLdcI4())
							{
								num = (uint)this.instructions[i].GetLdcI4Value();
							}
							int num2 = i + 1;
							if (this.instructions[i + 1].IsBr())
							{
								Instruction item2 = this.instructions[i + 1].Operand as Instruction;
								num2 = this.instructions.IndexOf(item2);
							}
							if (this.instructions[num2].IsLdcI4())
							{
								uint num3 = 0u;
								if (this.instructions[num2].IsLdcI4())
								{
									num3 = (uint)this.instructions[num2].GetLdcI4Value();
								}
								uint num4 = 0u;
								if ((this.instructions[num2 + 1].OpCode == OpCodes.Mul && this.instructions[num2 + 2].IsLdcI4()) || (this.instructions[num2 + 1].IsLdcI4() && this.instructions[num2 + 2].OpCode == OpCodes.Mul) || this.instructions[num2 + 1].OpCode == OpCodes.Xor)
								{
									if (this.instructions[num2 + 1].OpCode != OpCodes.Xor)
									{
										if (this.instructions[num2 + 1].OpCode == OpCodes.Mul && this.instructions[num2 + 2].IsLdcI4())
										{
											num4 = (uint)this.instructions[num2 + 2].GetLdcI4Value();
										}
										if (this.instructions[num2 + 1].IsLdcI4() && this.instructions[num2 + 2].OpCode == OpCodes.Mul)
										{
											num4 = (uint)this.instructions[num2 + 1].GetLdcI4Value();
										}
									}
									if (this.instructions[num2 + 3].OpCode == OpCodes.Xor || this.instructions[num2 + 1].OpCode == OpCodes.Xor)
									{
										for (int j = 0; j < this.toberemoved.Count; j++)
										{
											if ((this.instructions[num2 + 4].IsBr() && this.instructions[num2 + 4].Operand as Instruction == this.instructions[this.toberemoved[j]]) || num2 + 4 == this.toberemoved[j] || (this.instructions[num2 + 1].OpCode == OpCodes.Xor && num2 == this.toberemoved[j]))
											{
												uint num5;
												if (this.instructions[num2 + 1].OpCode == OpCodes.Xor)
												{
													num5 = (num ^ num3);
												}
												else if (this.instructions[num2 + 1].IsLdcI4() || this.instructions[num2 + 1].IsLdloc())
												{
													num5 = (num4 * num3 ^ num);
												}
												else
												{
													num5 = (num * num3 ^ num4);
												}
												uint num6;
												if (this.instructions[num2 + 1].OpCode != OpCodes.Xor)
												{
													num6 = (num5 ^ (uint)this.integer_values_1[j]);
												}
												else
												{
													num6 = num5;
												}
												uint num7 = num6 % (uint)this.for_rem[j];
												Instruction[] array = this.switchinstructions[j].Operand as Instruction[];
												Instruction item3 = array[(int)((UIntPtr)num7)];
												if (this.toberemovedindex.Contains(i))
												{
												}
												this.toberemovedindex.Add(i);
												this.toberemovedvalues.Add((int)num7);
												bool flag = false;
												if (this.brinstructions.IndexOf(item3) != -1)
												{
													flag = true;
												}
												if (flag)
												{
													this.brinstructions.Add(item3);
													this.InstructionParseNoLocal(this.instructions.IndexOf(item3));
													break;
												}
											}
										}
									}
								}
							}
						}
						else if (this.instructions[i].OpCode == OpCodes.Switch)
						{
							bool flag2;
							if (i - 4 < 0)
							{
								flag2 = false;
							}
							else
							{
								flag2 = false;
								for (int j = 0; j < this.toberemoved.Count; j++)
								{
									int num8 = this.toberemoved[j];
									if (i - 4 == this.toberemoved[j])
									{
										flag2 = true;
										break;
									}
								}
							}
							if (!flag2)
							{
								Instruction[] array2 = this.instructions[i].Operand as Instruction[];
								for (int j = 0; j < array2.Length; j++)
								{
									Instruction item4 = array2[j];
									this.InstructionParseNoLocal(this.instructions.IndexOf(item4));
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x0600214E RID: 8526 RVA: 0x0008FB58 File Offset: 0x0008EB58
		private void TextBox1DragDrop(object sender, DragEventArgs e)
		{
			try
			{
				Array array = (Array)e.Data.GetData(DataFormats.FileDrop);
				if (array != null)
				{
					string text = array.GetValue(0).ToString();
					int num = text.LastIndexOf(".");
					if (num != -1)
					{
						string text2 = text.Substring(num);
						text2 = text2.ToLower();
						if (text2 == ".exe" || text2 == ".dll")
						{
							base.Activate();
							this.textBox1.Text = text;
							int num2 = text.LastIndexOf("\\");
							if (num2 != -1)
							{
								this.DirectoryName = text.Remove(num2, text.Length - num2);
							}
							if (this.DirectoryName.Length == 2)
							{
								this.DirectoryName += "\\";
							}
						}
					}
				}
			}
			catch
			{
			}
		}

		// Token: 0x0600214F RID: 8527 RVA: 0x0008FC74 File Offset: 0x0008EC74
		private void TextBox1DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		// Token: 0x04000E84 RID: 3716
		private List<Instruction> brinstructions;

		// Token: 0x04000E8B RID: 3723
		private Button button1;

		// Token: 0x04000E8C RID: 3724
		private Button button2;

		// Token: 0x04000E8D RID: 3725
		private Button button3;

		// Token: 0x04000E87 RID: 3719
		private IContainer components = null;

		// Token: 0x04000E83 RID: 3715
		private List<Instruction> conditionalinstructions;

		// Token: 0x04000E78 RID: 3704
		public string DirectoryName = "";

		// Token: 0x04000E7D RID: 3709
		private List<int> for_rem;

		// Token: 0x04000E7F RID: 3711
		private IList<Instruction> instructions;

		// Token: 0x04000E7C RID: 3708
		private List<int> integer_values_1;

		// Token: 0x04000E89 RID: 3721
		private Label label1;

		// Token: 0x04000E8A RID: 3722
		private Label label2;

		// Token: 0x04000E86 RID: 3718
		private List<int> local_values;

		// Token: 0x04000E7A RID: 3706
		private Local local_variable = null;

		// Token: 0x04000E82 RID: 3714
		private MethodDef method;

		// Token: 0x04000E79 RID: 3705
		private List<MethodDef> methods = new List<MethodDef>();

		// Token: 0x04000E85 RID: 3717
		private List<Instruction> realbrinstructions;

		// Token: 0x04000E7E RID: 3710
		private List<Instruction> switchinstructions;

		// Token: 0x04000E88 RID: 3720
		private TextBox textBox1;

		// Token: 0x04000E7B RID: 3707
		private List<int> toberemoved;

		// Token: 0x04000E80 RID: 3712
		private List<int> toberemovedindex;

		// Token: 0x04000E81 RID: 3713
		private List<int> toberemovedvalues;
	}
}
