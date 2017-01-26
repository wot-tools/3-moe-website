class CreateNations < ActiveRecord::Migration[5.0]
  def change
    create_table :nations do |t|
      t.string :wgid
      t.string :name
	  #t.references :tank, foreign_key:true 

      t.timestamps
    end
  end
end
